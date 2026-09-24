using System.Diagnostics.CodeAnalysis;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Tuition;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
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
    IRentalNotificationDispatcher notifications,
    ILogger<StripeWebhookService> logger)
{
    private const string PaymentIntentSucceeded = "payment_intent.succeeded";
    private const string PaymentIntentPaymentFailed = "payment_intent.payment_failed";
    private const string ChargeRefunded = "charge.refunded";
    private const string ChargeRefundUpdated = "charge.refund.updated";

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
        if (await IsEventProcessedAsync(stripeEvent.Id, cancellationToken))
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

            case ChargeRefundUpdated when stripeEvent.Data.Object is Refund refund:
                await HandleChargeRefundUpdatedAsync(refund, stripeEvent.Id, rawJson, cancellationToken);
                break;

            default:
                logger.LogInformation("Ignoring unhandled Stripe webhook event type {EventType}", stripeEvent.Type);
                await RecordEventAsync(stripeEvent.Id, stripeEvent.Type, rawJson, cancellationToken);
                break;
        }
    }

    private async Task HandlePaymentIntentSucceededAsync(string paymentIntentId, string? chargeId, string eventId, string rawJson, CancellationToken cancellationToken)
    {
        await context.ExecuteInTransactionAsync(async () =>
        {
            if (await IsEventProcessedAsync(eventId, cancellationToken))
            {
                return;
            }

            var rentalPayment = await context.Set<RentalPayment>()
                .IgnoreQueryFilters()
                .Include(p => p.InstrumentRental)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);

            if (rentalPayment is not null)
            {
                if (rentalPayment.Status != PaymentStatus.Succeeded)
                {
                    LogMissingChargeId(chargeId, paymentIntentId);
                    rentalPayment.MarkSucceeded(chargeId, eventId, clock.UtcNow);
                    rentalPayment.InstrumentRental.MarkPaid(rentalPayment.AmountChargedCents, clock.UtcNow);
                    await notifications.DispatchPaymentSucceededAsync(rentalPayment.InstrumentRentalId, rentalPayment.AmountChargedCents, rentalPayment.Currency, cancellationToken);
                }

                await RecordEventAsync(eventId, PaymentIntentSucceeded, rawJson, cancellationToken);
                return;
            }

            var coursePayment = await context.Set<CoursePayment>()
                .IgnoreQueryFilters()
                .Include(p => p.Enrollment)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);

            if (coursePayment is not null)
            {
                if (coursePayment.Status != PaymentStatus.Succeeded)
                {
                    var now = clock.UtcNow;
                    var isActive = coursePayment.Enrollment.EnrollmentStatus == EnrollmentStatus.Active;
                    if (!isActive)
                    {
                        // Money was taken but the enrollment is not active: record the payment, grant no access.
                        logger.LogWarning("Skipping PaidUntil extension for non-active enrollment {EnrollmentId} on payment intent {PaymentIntentId}", coursePayment.EnrollmentId, paymentIntentId);
                    }
                    var (periodStart, periodEnd) = isActive
                        ? coursePayment.Enrollment.ExtendPaidUntil(now, TuitionOptions.PeriodDays)
                        : (now, now.AddDays(TuitionOptions.PeriodDays));
                    LogMissingChargeId(chargeId, paymentIntentId);
                    coursePayment.MarkSucceeded(chargeId, eventId, now, periodStart, periodEnd);
                }

                await RecordEventAsync(eventId, PaymentIntentSucceeded, rawJson, cancellationToken);
                return;
            }

            ThrowUnmatchedPaymentIntent(paymentIntentId, "succeeded");
        }, cancellationToken);
    }

    private async Task HandlePaymentIntentFailedAsync(string paymentIntentId, string eventId, string rawJson, CancellationToken cancellationToken)
    {
        await context.ExecuteInTransactionAsync(async () =>
        {
            if (await IsEventProcessedAsync(eventId, cancellationToken))
            {
                return;
            }

            var rentalPayment = await context.Set<RentalPayment>()
                .IgnoreQueryFilters()
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
                .IgnoreQueryFilters()
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

            ThrowUnmatchedPaymentIntent(paymentIntentId, "failed");
        }, cancellationToken);
    }

    private async Task HandleChargeRefundedAsync(Charge charge, string eventId, string rawJson, CancellationToken cancellationToken)
    {
        await context.ExecuteInTransactionAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(charge.PaymentIntentId))
            {
                logger.LogWarning("Charge {ChargeId} has no PaymentIntentId; ignoring refunded webhook", charge.Id);
                await RecordEventAsync(eventId, ChargeRefunded, rawJson, cancellationToken);
                return;
            }

            if (await IsEventProcessedAsync(eventId, cancellationToken))
            {
                return;
            }

            var rentalPayment = await context.Set<RentalPayment>()
                .IgnoreQueryFilters()
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == charge.PaymentIntentId, cancellationToken);

            if (rentalPayment is not null)
            {
                if (rentalPayment.Status is PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded && charge.AmountRefunded > 0)
                {
                    if (charge.Refunds?.Data is { Count: > 0 } refundList)
                    {
                        foreach (var refItem in refundList)
                        {
                            rentalPayment.ApplyRefund(refItem.Amount, refItem.Id, clock.UtcNow);
                        }
                    }

                    var alreadyRefunded = rentalPayment.RefundedCents ?? 0;
                    if (charge.AmountRefunded > alreadyRefunded)
                    {
                        var refundId = charge.Refunds?.Data?.FirstOrDefault()?.Id ?? rentalPayment.StripeRefundId;
                        rentalPayment.ApplyRefund(charge.AmountRefunded - alreadyRefunded, refundId, clock.UtcNow);
                    }
                }

                await RecordEventAsync(eventId, ChargeRefunded, rawJson, cancellationToken);
                return;
            }

            var coursePayment = await context.Set<CoursePayment>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == charge.PaymentIntentId, cancellationToken);

            if (coursePayment is not null)
            {
                logger.LogInformation("Charge {ChargeId} refunded for course payment {CoursePaymentId}; leaving payment as-is", charge.Id, coursePayment.Id);
                await RecordEventAsync(eventId, ChargeRefunded, rawJson, cancellationToken);
                return;
            }

            ThrowUnmatchedPaymentIntent(charge.PaymentIntentId, "refunded");
        }, cancellationToken);
    }

    private async Task HandleChargeRefundUpdatedAsync(Refund refund, string eventId, string rawJson, CancellationToken cancellationToken)
    {
        await context.ExecuteInTransactionAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(refund.PaymentIntentId))
            {
                logger.LogWarning("Refund {RefundId} has no PaymentIntentId; ignoring refund.updated webhook", refund.Id);
                await RecordEventAsync(eventId, ChargeRefundUpdated, rawJson, cancellationToken);
                return;
            }

            if (await IsEventProcessedAsync(eventId, cancellationToken))
            {
                return;
            }

            var rentalPayment = await context.Set<RentalPayment>()
                .IgnoreQueryFilters()
                .Include(p => p.InstrumentRental)
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == refund.PaymentIntentId, cancellationToken);

            if (rentalPayment is null)
            {
                var coursePayment = await context.Set<CoursePayment>()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.StripePaymentIntentId == refund.PaymentIntentId, cancellationToken);

                if (coursePayment is not null)
                {
                    logger.LogInformation("Refund {RefundId} updated for course payment {CoursePaymentId}; leaving payment as-is", refund.Id, coursePayment.Id);
                    await RecordEventAsync(eventId, ChargeRefundUpdated, rawJson, cancellationToken);
                    return;
                }

                ThrowUnmatchedPaymentIntent(refund.PaymentIntentId, "refund.updated");
            }

            if (rentalPayment.Status is PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded)
            {
                // The charge.refunded fallback records a null refund id when the payload carries no refund list.
                var counted = (!string.IsNullOrWhiteSpace(refund.Id) && rentalPayment.Refunds.Any(r => r.StripeRefundId == refund.Id))
                    || rentalPayment.StripeRefundId == refund.Id
                    || (rentalPayment.StripeRefundId is null && (rentalPayment.RefundedCents ?? 0) >= refund.Amount);

                if (refund.Status == "succeeded")
                {
                    if (counted)
                    {
                        logger.LogInformation("Refund {RefundId} already applied to payment {PaymentId}; skipping", refund.Id, rentalPayment.Id);
                    }
                    else
                    {
                        rentalPayment.ApplyRefund(refund.Amount, refund.Id, clock.UtcNow);
                    }
                }
                else if (refund.Status == "failed")
                {
                    if (counted)
                    {
                        rentalPayment.ReverseRefund(refund.Amount, refund.Id);
                        logger.LogWarning("Refund {RefundId} failed for payment {PaymentId}; the counted refund was reversed", refund.Id, rentalPayment.Id);
                    }
                    else
                    {
                        logger.LogWarning("Refund {RefundId} failed for payment {PaymentId}; no refund was applied", refund.Id, rentalPayment.Id);
                    }
                }
            }
            else
            {
                logger.LogInformation("Refund {RefundId} update for payment {PaymentId} in status {PaymentStatus}; ignoring", refund.Id, rentalPayment.Id, rentalPayment.Status);
            }

            await RecordEventAsync(eventId, ChargeRefundUpdated, rawJson, cancellationToken);
        }, cancellationToken);
    }

    private void LogMissingChargeId(string? chargeId, string paymentIntentId)
    {
        if (chargeId is null)
        {
            logger.LogWarning("PaymentIntent {PaymentIntentId} succeeded without a charge id", paymentIntentId);
        }
    }

    [DoesNotReturn]
    private void ThrowUnmatchedPaymentIntent(string paymentIntentId, string eventKind)
    {
        logger.LogWarning("PaymentIntent {PaymentIntentId} not found for {EventKind} webhook; returning 503 for Stripe retry.", paymentIntentId, eventKind);
        throw new PaymentNotYetVisibleException(Messages.PaymentNotYetVisible);
    }

    private Task<bool> IsEventProcessedAsync(string eventId, CancellationToken cancellationToken) =>
        context.Set<StripeWebhookEvent>().AnyAsync(e => e.StripeEventId == eventId, cancellationToken);

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
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict processing Stripe webhook event {EventId}", eventId);
            throw;
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, DbConstraintNames.StripeWebhookEventStripeEventIdUniqueIndex))
        {
            logger.LogInformation("Duplicate Stripe webhook event {EventId} ignored", eventId);
        }
    }
}
