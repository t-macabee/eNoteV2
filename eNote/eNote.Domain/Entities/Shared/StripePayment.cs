using eNote.Domain.Enums;

namespace eNote.Domain.Entities.Shared;

public abstract class StripePayment : AuditableEntity
{
    public string StripePaymentIntentId { get; private set; } = null!;
    public string? StripeChargeId { get; private set; }
    public string? StripeEventId { get; private set; }
    public long AmountChargedCents { get; private set; }
    public string Currency { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public long? RefundedCents { get; private set; }
    public string? StripeRefundId { get; private set; }

    public byte[] Version { get; private set; } = null!;

    private readonly List<PaymentRefund> _refunds = [];
    public IReadOnlyCollection<PaymentRefund> Refunds => _refunds.AsReadOnly();

    protected StripePayment()
    {
    }

    protected StripePayment(
        string stripePaymentIntentId,
        long amountChargedCents,
        string currency,
        PaymentStatus status)
    {
        StripePaymentIntentId = stripePaymentIntentId;
        AmountChargedCents = amountChargedCents;
        Currency = currency;
        Status = status;
    }

    protected void MarkSucceededCore(string? stripeChargeId, string stripeEventId, DateTime paidAt)
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

    public void MarkCanceled()
    {
        Status = PaymentStatus.Canceled;
    }

    public bool ApplyRefund(long refundedCents, string stripeRefundId, DateTime refundedAt)
    {
        var existing = _refunds.FirstOrDefault(r => r.StripeRefundId == stripeRefundId);
        if (existing is not null)
        {
            return false;
        }

        _refunds.Add(CreateRefund(stripeRefundId, refundedCents, refundedAt));

        RefundedCents = _refunds.Sum(r => r.AmountCents);
        StripeRefundId = stripeRefundId;
        RefundedAt = refundedAt;
        Status = RefundedCents >= AmountChargedCents
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;

        return true;
    }

    public bool ReverseRefund(string stripeRefundId)
    {
        var existing = _refunds.FirstOrDefault(r => r.StripeRefundId == stripeRefundId);
        if (existing is null)
        {
            return false;
        }

        _refunds.Remove(existing);

        if (_refunds.Count > 0)
        {
            RefundedCents = _refunds.Sum(r => r.AmountCents);
            StripeRefundId = _refunds.Last().StripeRefundId;
            RefundedAt = _refunds.Last().AppliedAtUtc;
            Status = RefundedCents >= AmountChargedCents
                ? PaymentStatus.Refunded
                : PaymentStatus.PartiallyRefunded;
            return true;
        }

        RefundedCents = null;
        StripeRefundId = null;
        RefundedAt = null;
        Status = PaymentStatus.Succeeded;
        return true;
    }

    protected abstract PaymentRefund CreateRefund(string stripeRefundId, long amountCents, DateTime appliedAtUtc);
}
