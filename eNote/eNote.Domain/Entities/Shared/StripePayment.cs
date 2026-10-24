using eNote.Domain.Enums;

namespace eNote.Domain.Entities.Shared;

public abstract class StripePayment : AuditableEntity, IStripePaymentRow
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

    public void ApplyRefund(long refundedCents, string? stripeRefundId, DateTime refundedAt)
    {
        if (!string.IsNullOrWhiteSpace(stripeRefundId))
        {
            var existing = _refunds.FirstOrDefault(r => r.StripeRefundId == stripeRefundId);
            if (existing is null)
            {
                _refunds.Add(CreateRefund(stripeRefundId, refundedCents, refundedAt));
            }
            else if (existing.AmountCents < refundedCents)
            {
                _refunds.Remove(existing);
                _refunds.Add(CreateRefund(stripeRefundId, refundedCents, refundedAt));
            }
            else
            {
                return;
            }
        }

        RefundedCents = _refunds.Count > 0
            ? _refunds.Sum(r => r.AmountCents)
            : (RefundedCents ?? 0) + refundedCents;

        StripeRefundId = stripeRefundId ?? StripeRefundId;
        RefundedAt = refundedAt;
        Status = RefundedCents >= AmountChargedCents
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
    }

    public void ReverseRefund(long refundedCents, string? stripeRefundId = null)
    {
        if (!string.IsNullOrWhiteSpace(stripeRefundId))
        {
            var existing = _refunds.FirstOrDefault(r => r.StripeRefundId == stripeRefundId);
            if (existing is not null)
            {
                _refunds.Remove(existing);
            }
        }

        if (_refunds.Count > 0)
        {
            RefundedCents = _refunds.Sum(r => r.AmountCents);
            StripeRefundId = _refunds.Last().StripeRefundId;
            RefundedAt = _refunds.Last().AppliedAtUtc;
            Status = RefundedCents >= AmountChargedCents
                ? PaymentStatus.Refunded
                : PaymentStatus.PartiallyRefunded;
            return;
        }

        var remaining = Math.Max(0, (RefundedCents ?? 0) - refundedCents);
        if (remaining == 0)
        {
            RefundedCents = null;
            StripeRefundId = null;
            RefundedAt = null;
            Status = PaymentStatus.Succeeded;
            return;
        }

        RefundedCents = remaining;
        Status = PaymentStatus.PartiallyRefunded;
    }

    protected abstract PaymentRefund CreateRefund(string stripeRefundId, long amountCents, DateTime appliedAtUtc);
}
