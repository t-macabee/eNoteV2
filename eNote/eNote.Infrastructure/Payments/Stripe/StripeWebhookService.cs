using System.Diagnostics.CodeAnalysis;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Academic.Tuition;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Rentals.Payments.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;
using StripeEvent = Stripe.Event;

namespace eNote.Infrastructure.Payments.Stripe;

public sealed class StripeWebhookService(
    IAppDbContext context,
    IClock clock,
    StripeOptions options,
    IPaymentGateway paymentGateway,
    IRentalNotificationDispatcher notifications,
    IEnrollmentNotificationDispatcher enrollmentNotifications,
    ILogger<StripeWebhookService> logger)
{
    private const string PaymentIntentSucceeded = "payment_intent.succeeded";
    private const string PaymentIntentPaymentFailed = "payment_intent.payment_failed";
    private const string ChargeRefunded = "charge.refunded";
    private const string ChargeRefundUpdated = "charge.refund.updated";
    private const string RefundSucceeded = "succeeded";
    private const string RefundFailed = "failed";

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

            var payment = await LoadPaymentAsync(paymentIntentId, cancellationToken);

            switch (payment)
            {
                case RentalPayment rentalPayment:
                    await HandleRentalSucceededAsync(rentalPayment, chargeId, eventId, rawJson, cancellationToken);
                    return;
                case CoursePayment coursePayment:
                    await HandleCourseSucceededAsync(coursePayment, chargeId, eventId, rawJson, cancellationToken);
                    return;
                case null:
                    ThrowUnmatchedPaymentIntent(paymentIntentId, "succeeded");
                    return;
            }
        }, cancellationToken);
    }

    private async Task HandleRentalSucceededAsync(RentalPayment payment, string? chargeId, string eventId, string rawJson, CancellationToken cancellationToken)
    {
        if (payment.Status != PaymentStatus.Succeeded)
        {
            LogMissingChargeId(chargeId, payment.StripePaymentIntentId);
            payment.MarkSucceeded(chargeId, eventId, clock.UtcNow);

            if (payment.InstrumentRental.IsPaid)
            {
                await RefundDuplicatePaymentAsync(payment, cancellationToken);
                await notifications.DispatchDuplicatePaymentRefundedAsync(payment.InstrumentRentalId, payment.AmountChargedCents, payment.Currency, cancellationToken);
            }
            else
            {
                payment.InstrumentRental.MarkPaid(payment.AmountChargedCents, clock.UtcNow);
                await notifications.DispatchPaymentSucceededAsync(payment.InstrumentRentalId, payment.AmountChargedCents, payment.Currency, cancellationToken);
            }
        }

        await RecordEventAsync(eventId, PaymentIntentSucceeded, rawJson, cancellationToken);
    }

    private async Task HandleCourseSucceededAsync(CoursePayment payment, string? chargeId, string eventId, string rawJson, CancellationToken cancellationToken)
    {
        if (payment.Status != PaymentStatus.Succeeded)
        {
            var now = clock.UtcNow;
            var enrollment = payment.Enrollment;
            LogMissingChargeId(chargeId, payment.StripePaymentIntentId);

            if (CanGrantAccess(payment, now))
            {
                var (periodStart, periodEnd) = enrollment.ExtendPaidUntil(now, TuitionOptions.PeriodDays);
                payment.MarkSucceeded(chargeId, eventId, now, periodStart, periodEnd);
                await enrollmentNotifications.DispatchTuitionPaidAsync(enrollment.Id, enrollment.Student.AppUserId, enrollment.Course.Name, payment.AmountChargedCents, payment.Currency, periodEnd);
            }
            else
            {
                logger.LogWarning("Tuition payment {PaymentIntentId} cannot grant access; refunding it instead", payment.StripePaymentIntentId);
                payment.MarkSucceeded(chargeId, eventId, now, periodStart: null, periodEnd: null);
                await RefundDuplicatePaymentAsync(payment, cancellationToken);
                await enrollmentNotifications.DispatchTuitionRefundedAsync(enrollment.Id, enrollment.Student.AppUserId, enrollment.Course.Name, payment.AmountChargedCents, payment.Currency);
            }
        }

        await RecordEventAsync(eventId, PaymentIntentSucceeded, rawJson, cancellationToken);
    }

    private static bool CanGrantAccess(CoursePayment payment, DateTime now) =>
        payment.Status != PaymentStatus.Canceled
        && payment.Enrollment.EnrollmentStatus == EnrollmentStatus.Active
        && payment.Enrollment.Course is { IsActive: true, IsPublished: true } course
        && !course.HasEnded(now);

    private async Task HandlePaymentIntentFailedAsync(string paymentIntentId, string eventId, string rawJson, CancellationToken cancellationToken)
    {
        await context.ExecuteInTransactionAsync(async () =>
        {
            if (await IsEventProcessedAsync(eventId, cancellationToken))
            {
                return;
            }

            var payment = await LoadPaymentAsync(paymentIntentId, cancellationToken);

            if (payment is null)
            {
                ThrowUnmatchedPaymentIntent(paymentIntentId, "failed");
                return;
            }

            if (payment.Status is PaymentStatus.RequiresAction or PaymentStatus.Failed)
            {
                payment.MarkFailed(eventId);
            }

            await RecordEventAsync(eventId, PaymentIntentPaymentFailed, rawJson, cancellationToken);
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

            var payment = await LoadPaymentAsync(charge.PaymentIntentId, cancellationToken);

            if (payment is null)
            {
                ThrowUnmatchedPaymentIntent(charge.PaymentIntentId, "refunded");
                return;
            }

            ApplyChargeRefund(payment, charge);

            if (payment is CoursePayment coursePayment)
            {
                coursePayment.Enrollment.RevokeRefundedAccessPeriod(coursePayment);
            }

            await RecordEventAsync(eventId, ChargeRefunded, rawJson, cancellationToken);
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

            var payment = await LoadPaymentAsync(refund.PaymentIntentId, cancellationToken);

            if (payment is null)
            {
                ThrowUnmatchedPaymentIntent(refund.PaymentIntentId, "refund.updated");
                return;
            }

            ApplyRefundUpdate(payment, refund);

            await RecordEventAsync(eventId, ChargeRefundUpdated, rawJson, cancellationToken);
        }, cancellationToken);
    }

    private void ApplyChargeRefund(StripePayment payment, Charge charge)
    {
        if (payment.Status is not (PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded) || charge.AmountRefunded <= 0)
        {
            return;
        }

        if (charge.Refunds?.Data is { Count: > 0 } refundList)
        {
            foreach (var refundItem in refundList)
            {
                payment.ApplyRefund(refundItem.Amount, refundItem.Id, clock.UtcNow);
            }
        }

        var alreadyRefunded = payment.RefundedCents ?? 0;
        if (charge.AmountRefunded > alreadyRefunded)
        {
            var refundId = charge.Refunds?.Data?.FirstOrDefault()?.Id ?? payment.StripeRefundId;
            payment.ApplyRefund(charge.AmountRefunded - alreadyRefunded, refundId, clock.UtcNow);
        }
    }

    private void ApplyRefundUpdate(StripePayment payment, Refund refund)
    {
        if (payment.Status is not (PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded))
        {
            logger.LogInformation("Refund {RefundId} update for payment {PaymentId} in status {PaymentStatus}; ignoring", refund.Id, payment.Id, payment.Status);
            return;
        }

        var counted = IsRefundCounted(payment, refund);

        if (refund.Status == RefundSucceeded)
        {
            if (counted)
            {
                logger.LogInformation("Refund {RefundId} already applied to payment {PaymentId}; skipping", refund.Id, payment.Id);
                return;
            }

            payment.ApplyRefund(refund.Amount, refund.Id, clock.UtcNow);

            if (payment is CoursePayment refundedCoursePayment)
            {
                refundedCoursePayment.Enrollment.RevokeRefundedAccessPeriod(refundedCoursePayment);
            }

            return;
        }

        if (refund.Status != RefundFailed)
        {
            return;
        }

        if (!counted)
        {
            logger.LogWarning("Refund {RefundId} failed for payment {PaymentId}; no refund was applied", refund.Id, payment.Id);
            return;
        }

        payment.ReverseRefund(refund.Amount, refund.Id);

        if (payment is CoursePayment reversedCoursePayment)
        {
            reversedCoursePayment.Enrollment.RestoreRefundedAccessPeriod(reversedCoursePayment);
        }

        logger.LogWarning("Refund {RefundId} failed for payment {PaymentId}; the counted refund was reversed", refund.Id, payment.Id);
    }

    private static bool IsRefundCounted(StripePayment payment, Refund refund) =>
        (!string.IsNullOrWhiteSpace(refund.Id) && payment.Refunds.Any(r => r.StripeRefundId == refund.Id))
        || payment.StripeRefundId == refund.Id
        || (payment.StripeRefundId is null && (payment.RefundedCents ?? 0) >= refund.Amount);

    private async Task<StripePayment?> LoadPaymentAsync(string paymentIntentId, CancellationToken cancellationToken)
    {
        var rentalPayment = await context.Set<RentalPayment>()
            .IgnoreQueryFilters()
            .Include(p => p.InstrumentRental)
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);

        if (rentalPayment is not null)
        {
            return rentalPayment;
        }

        return await context.Set<CoursePayment>()
            .IgnoreQueryFilters()
            .Include(p => p.Enrollment)
            .ThenInclude(e => e.Student)
            .Include(p => p.Enrollment)
            .ThenInclude(e => e.Course)
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, cancellationToken);
    }

    private async Task RefundDuplicatePaymentAsync(StripePayment payment, CancellationToken cancellationToken)
    {
        // Stripe is called inside the webhook transaction on purpose: if the transaction fails, Stripe retries the
        // event, and the idempotency key keeps a second refund from being created.
        var refund = await paymentGateway.CreateRefundAsync(
            payment.StripePaymentIntentId,
            payment.AmountChargedCents,
            "duplicate",
            $"duplicate:{payment.StripePaymentIntentId}",
            cancellationToken);

        if (refund.Status == RefundSucceeded)
        {
            payment.ApplyRefund(refund.AmountCents, refund.Id, clock.UtcNow);
        }
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
