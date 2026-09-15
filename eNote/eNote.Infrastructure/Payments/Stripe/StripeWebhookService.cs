using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Tuition;
using eNote.Application.Features.Rentals.Payments.Services;
using eNote.Domain.Entities.Academic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;
using StripeEvent = Stripe.Event;

namespace eNote.Infrastructure.Payments.Stripe;

/// <summary>
/// Verifies Stripe webhook signatures, applies payment_intent/charge side effects
/// idempotently, and records every processed event in the <see cref="StripeWebhookEvent"/>
/// table so Stripe replays cannot double-apply.
/// </summary>
public sealed class StripeWebhookService(
    IAppDbContext context,
    IClock clock,
    StripeOptions options,
    ILogger<StripeWebhookService> logger)
{
    private const string PaymentIntentSucceeded = "payment_intent.succeeded";
    private const string PaymentIntentPaymentFailed = "payment_intent.payment_failed";
    private const string ChargeRefunded = "charge.refunded";

    public async Task HandleAsync(string rawJson, string signatureHeader, CancellationToken cancellationToken = default)
    {
        StripeEvent stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(rawJson, signatureHeader, options.WebhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Stripe webhook signature verification failed");
            throw new BusinessException(Messages.StripeWebhookSignatureInvalid);
        }

        await HandleAsync(stripeEvent, rawJson, cancellationToken);
    }

    public async Task HandleAsync(StripeEvent stripeEvent, string rawJson, CancellationToken cancellationToken = default)
    {
        // Fast-path replay guard; the transactional handlers below repeat it to close races.
        if (await context.Set<StripeWebhookEvent>().AnyAsync(e => e.StripeEventId == stripeEvent.Id, cancellationToken))
        {
            return;
        }

        switch (stripeEvent.Type)
        {
            case PaymentIntentSucceeded when stripeEvent.Data.Object is PaymentIntent paymentIntent:
                await HandlePaymentIntentSucceededAsync(paymentIntent.Id, paymentIntent.LatestChargeId, stripeEvent.Id, rawJson, cancellationToken);
                break;

            case PaymentIntentPaymentFailed when stripeEvent.Data.Object is PaymentIntent failedIntent:
                await HandlePaymentIntentFailedAsync(failedIntent.Id, stripeEvent.Id, rawJson, cancellationToken);
                break;

            case ChargeRefunded when stripeEvent.Data.Object is Charge charge:
                await HandleChargeRefundedAsync(charge, stripeEvent.Id, rawJson, cancellationToken);
                break;

            default:
                logger.LogInformation("Ignoring unhandled Stripe webhook event type {EventType}", stripeEvent.Type);
                break;
        }
    }

    private async Task HandlePaymentIntentSucceededAsync(string paymentIntentId, string? chargeId, string eventId, string rawJson, CancellationToken cancellationToken)
    {
        await context.ExecuteInTransactionAsync(async () =>
        {
            if (await context.Set<StripeWebhookEvent>().AnyAsync(e => e.StripeEventId == eventId, cancellationToken))
            {
                return;
            }

            var rentalPayment = await context.Set<RentalPayment>()
                .Include(p => p.InstrumentRental)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);

            if (rentalPayment is not null)
            {
                if (rentalPayment.Status != PaymentStatus.Succeeded)
                {
                    LogMissingChargeId(chargeId, paymentIntentId);
                    rentalPayment.MarkSucceeded(chargeId, eventId, clock.UtcNow);
                    rentalPayment.InstrumentRental.MarkPaid(rentalPayment.AmountChargedCents, clock.UtcNow);
                }

                await RecordEventAsync(eventId, PaymentIntentSucceeded, rawJson, cancellationToken);
                return;
            }

            var coursePayment = await context.Set<CoursePayment>()
                .Include(p => p.Enrollment)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);

            if (coursePayment is not null)
            {
                if (coursePayment.Status != PaymentStatus.Succeeded)
                {
                    var now = clock.UtcNow;
                    var (periodStart, periodEnd) = coursePayment.Enrollment.ExtendPaidUntil(now, TuitionOptions.PeriodDays);
                    LogMissingChargeId(chargeId, paymentIntentId);
                    coursePayment.MarkSucceeded(chargeId, eventId, now, periodStart, periodEnd);
                }

                await RecordEventAsync(eventId, PaymentIntentSucceeded, rawJson, cancellationToken);
                return;
            }

            logger.LogWarning("PaymentIntent {PaymentIntentId} not found for succeeded webhook", paymentIntentId);
        }, cancellationToken);
    }

    private async Task HandlePaymentIntentFailedAsync(string paymentIntentId, string eventId, string rawJson, CancellationToken cancellationToken)
    {
        await context.ExecuteInTransactionAsync(async () =>
        {
            if (await context.Set<StripeWebhookEvent>().AnyAsync(e => e.StripeEventId == eventId, cancellationToken))
            {
                return;
            }

            var rentalPayment = await context.Set<RentalPayment>()
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);

            if (rentalPayment is not null)
            {
                if (rentalPayment.Status is not (PaymentStatus.Succeeded or PaymentStatus.Refunded or PaymentStatus.PartiallyRefunded))
                {
                    rentalPayment.MarkFailed(eventId);
                }

                await RecordEventAsync(eventId, PaymentIntentPaymentFailed, rawJson, cancellationToken);
                return;
            }

            var coursePayment = await context.Set<CoursePayment>()
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);

            if (coursePayment is not null)
            {
                if (coursePayment.Status != PaymentStatus.Succeeded)
                {
                    coursePayment.MarkFailed(eventId);
                }

                await RecordEventAsync(eventId, PaymentIntentPaymentFailed, rawJson, cancellationToken);
                return;
            }

            logger.LogWarning("PaymentIntent {PaymentIntentId} not found for failed webhook", paymentIntentId);
        }, cancellationToken);
    }

    private async Task HandleChargeRefundedAsync(Charge charge, string eventId, string rawJson, CancellationToken cancellationToken)
    {
        await context.ExecuteInTransactionAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(charge.PaymentIntentId))
            {
                logger.LogWarning("Charge {ChargeId} has no PaymentIntentId; ignoring refunded webhook", charge.Id);
                return;
            }

            if (await context.Set<StripeWebhookEvent>().AnyAsync(e => e.StripeEventId == eventId, cancellationToken))
            {
                return;
            }

            var rentalPayment = await context.Set<RentalPayment>()
                .Include(p => p.InstrumentRental)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == charge.PaymentIntentId, cancellationToken);

            if (rentalPayment is not null)
            {
                if (rentalPayment.Status == PaymentStatus.Succeeded && charge.AmountRefunded > 0)
                {
                    var refundId = charge.Refunds?.Data?.FirstOrDefault()?.Id ?? rentalPayment.StripeRefundId;
                    if (refundId is null)
                    {
                        logger.LogWarning("Charge {ChargeId} refunded without a refund id", charge.Id);
                    }

                    var alreadyRefunded = rentalPayment.RefundedCents ?? 0;

                    if (charge.AmountRefunded > alreadyRefunded)
                    {
                        rentalPayment.ApplyRefund(charge.AmountRefunded - alreadyRefunded, refundId, clock.UtcNow);
                    }
                }

                await RecordEventAsync(eventId, ChargeRefunded, rawJson, cancellationToken);
                return;
            }

            var coursePayment = await context.Set<CoursePayment>()
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == charge.PaymentIntentId, cancellationToken);

            if (coursePayment is not null)
            {
                logger.LogInformation("Charge {ChargeId} refunded for course payment {CoursePaymentId}; leaving payment as-is", charge.Id, coursePayment.Id);
                await RecordEventAsync(eventId, ChargeRefunded, rawJson, cancellationToken);
                return;
            }

            logger.LogWarning("PaymentIntent {PaymentIntentId} not found for refunded webhook", charge.PaymentIntentId);
        }, cancellationToken);
    }

    private void LogMissingChargeId(string? chargeId, string paymentIntentId)
    {
        if (chargeId is null)
        {
            logger.LogWarning("PaymentIntent {PaymentIntentId} succeeded without a charge id", paymentIntentId);
        }
    }

    private async Task RecordEventAsync(string eventId, string eventType, string rawJson, CancellationToken cancellationToken)
    {
        context.Set<StripeWebhookEvent>().Add(new StripeWebhookEvent(eventId, eventType, rawJson, clock.UtcNow));
        await SaveAsync(eventId, cancellationToken);
    }

    private async Task SaveAsync(string eventId, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException?.Message?.Contains(DbConstraintNames.StripeWebhookEventStripeEventIdUniqueIndex) == true
            || ex.InnerException?.Message?.Contains(DbConstraintNames.RentalPaymentStripeEventIdUniqueIndex) == true
            || ex.InnerException?.Message?.Contains(DbConstraintNames.CoursePaymentStripeEventIdUniqueIndex) == true
            || ex.InnerException?.Message?.Contains(DbConstraintNames.CoursePaymentPaymentIntentIdUniqueIndex) == true)
        {
            logger.LogInformation("Duplicate Stripe webhook event {EventId} ignored", eventId);
        }
    }
}
