using eNote.Domain.Enums;

namespace eNote.Domain.Entities.Academic;

public sealed class CoursePayment : StripePayment
{
    public int EnrollmentId { get; private set; }
    public Enrollment Enrollment { get; private set; } = null!;

    public DateTime? PeriodStart { get; private set; }
    public DateTime? PeriodEnd { get; private set; }

    private CoursePayment()
    {
    }

    public CoursePayment(
        int enrollmentId,
        string stripePaymentIntentId,
        long amountChargedCents,
        string currency,
        PaymentStatus status)
        : base(stripePaymentIntentId, amountChargedCents, currency, status)
    {
        EnrollmentId = enrollmentId;
    }

    public void MarkSucceeded(
        string? stripeChargeId,
        string stripeEventId,
        DateTime paidAt,
        DateTime? periodStart,
        DateTime? periodEnd)
    {
        MarkSucceededCore(stripeChargeId, stripeEventId, paidAt);
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
    }

    protected override PaymentRefund CreateRefund(string stripeRefundId, long amountCents, DateTime appliedAtUtc)
    {
        return new PaymentRefund(this, stripeRefundId, amountCents, appliedAtUtc);
    }
}
