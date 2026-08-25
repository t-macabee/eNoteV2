using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.Payments.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;

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
        Event stripeEvent;

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

    public async Task HandleAsync(Event stripeEvent, string rawJson, CancellationToken cancellationToken = default)
    {
        // Fast-path replay guard; the transactional handlers below repeat it to close races.
        if (await context.Set<StripeWebhookEvent>().AnyAsync(e => e.StripeEventId == stripeEvent.Id, cancellationToken))
        {
            return;
        }

        if (await context.Set<RentalPayment>().AnyAsync(p => p.StripeEventId == stripeEvent.Id, cancellationToken))
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
            var payment = await context.Set<RentalPayment>()
                .Include(p => p.InstrumentRental)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);

            if (payment is null)
            {
                logger.LogWarning("PaymentIntent {PaymentIntentId} not found for succeeded webhook", paymentIntentId);
                return;
            }

            if (await context.Set<StripeWebhookEvent>().AnyAsync(e => e.StripeEventId == eventId, cancellationToken))
            {
                return;
            }

            if (payment.Status != PaymentStatus.Succeeded)
            {
                payment.MarkSucceeded(chargeId ?? payment.StripeChargeId!, eventId, clock.UtcNow);
                payment.InstrumentRental.MarkPaid(payment.AmountChargedCents, clock.UtcNow);
            }

            context.Set<StripeWebhookEvent>().Add(new StripeWebhookEvent(eventId, PaymentIntentSucceeded, rawJson, clock.UtcNow));
            await SaveAsync(eventId, cancellationToken);
        }, cancellationToken);
    }

    private async Task HandlePaymentIntentFailedAsync(string paymentIntentId, string eventId, string rawJson, CancellationToken cancellationToken)
    {
        await context.ExecuteInTransactionAsync(async () =>
        {
            var payment = await context.Set<RentalPayment>()
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);

            if (payment is null)
            {
                logger.LogWarning("PaymentIntent {PaymentIntentId} not found for failed webhook", paymentIntentId);
                return;
            }

            if (await context.Set<StripeWebhookEvent>().AnyAsync(e => e.StripeEventId == eventId, cancellationToken))
            {
                return;
            }

            if (payment.Status is not (PaymentStatus.Succeeded or PaymentStatus.Refunded or PaymentStatus.PartiallyRefunded))
            {
                payment.MarkFailed(eventId);
            }

            context.Set<StripeWebhookEvent>().Add(new StripeWebhookEvent(eventId, PaymentIntentPaymentFailed, rawJson, clock.UtcNow));
            await SaveAsync(eventId, cancellationToken);
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

            var payment = await context.Set<RentalPayment>()
                .Include(p => p.InstrumentRental)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == charge.PaymentIntentId, cancellationToken);

            if (payment is null)
            {
                logger.LogWarning("PaymentIntent {PaymentIntentId} not found for refunded webhook", charge.PaymentIntentId);
                return;
            }

            if (await context.Set<StripeWebhookEvent>().AnyAsync(e => e.StripeEventId == eventId, cancellationToken))
            {
                return;
            }

            if (payment.Status == PaymentStatus.Succeeded && charge.AmountRefunded > 0)
            {
                var refundId = charge.Refunds?.Data?.FirstOrDefault()?.Id ?? payment.StripeRefundId ?? $"re_{eventId}";
                var alreadyRefunded = payment.RefundedCents ?? 0;

                if (charge.AmountRefunded > alreadyRefunded)
                {
                    payment.ApplyRefund(charge.AmountRefunded - alreadyRefunded, refundId, clock.UtcNow);
                }
            }

            context.Set<StripeWebhookEvent>().Add(new StripeWebhookEvent(eventId, ChargeRefunded, rawJson, clock.UtcNow));
            await SaveAsync(eventId, cancellationToken);
        }, cancellationToken);
    }

    private async Task SaveAsync(string eventId, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException?.Message?.Contains(DbConstraintNames.StripeWebhookEventStripeEventIdUniqueIndex) == true
            || ex.InnerException?.Message?.Contains(DbConstraintNames.RentalPaymentStripeEventIdUniqueIndex) == true)
        {
            // A concurrent delivery of the same event won the race; treat this one as handled.
            logger.LogInformation("Duplicate Stripe webhook event {EventId} ignored", eventId);
        }
    }

}
