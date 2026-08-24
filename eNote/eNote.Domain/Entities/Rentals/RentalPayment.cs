using eNote.Domain.Entities.Shared;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities.Rentals;

/// <summary>
/// One Stripe payment attempt for an instrument rental. A rental may have several
/// attempts over its lifetime; the unique Stripe PaymentIntent id keeps them idempotent.
/// </summary>
public sealed class RentalPayment : AuditableEntity, ITenantScoped
{
    public int InstrumentRentalId { get; private set; }
    public InstrumentRental InstrumentRental { get; private set; } = null!;
    public int MusicStoreId { get; private set; }

    public string StripePaymentIntentId { get; private set; } = null!;
    public string? StripeChargeId { get; private set; }
    public long AmountChargedCents { get; private set; }
    public string Currency { get; private set; } = "eur";
    public PaymentStatus Status { get; private set; }
    public string? StripeEventId { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public long? RefundedCents { get; private set; }
    public string? StripeRefundId { get; private set; }

    private RentalPayment()
    {
    }

    public RentalPayment(
        int instrumentRentalId,
        int musicStoreId,
        string stripePaymentIntentId,
        long amountChargedCents,
        string currency,
        PaymentStatus status)
    {
        InstrumentRentalId = instrumentRentalId;
        MusicStoreId = musicStoreId;
        StripePaymentIntentId = stripePaymentIntentId;
        AmountChargedCents = amountChargedCents;
        Currency = currency;
        Status = status;
    }

    public void MarkSucceeded(string stripeChargeId, string stripeEventId, DateTime paidAt)
    {
        StripeChargeId = stripeChargeId;
        StripeEventId = stripeEventId;
        Status = PaymentStatus.Succeeded;
        PaidAt = paidAt;
    }

    public void MarkFailed(string stripeEventId)
    {
        StripeEventId = stripeEventId;
        Status = PaymentStatus.Failed;
    }

    public void MarkCanceled(string stripeEventId)
    {
        StripeEventId = stripeEventId;
        Status = PaymentStatus.Canceled;
    }

    public void ApplyRefund(long refundedCents, string stripeRefundId, DateTime refundedAt)
    {
        RefundedCents = (RefundedCents ?? 0) + refundedCents;
        StripeRefundId = stripeRefundId;
        RefundedAt = refundedAt;
        Status = RefundedCents >= AmountChargedCents
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
    }
}
