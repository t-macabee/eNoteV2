using eNote.Domain.Enums;

namespace eNote.Domain.Entities.Academic;

public sealed class CoursePayment : AuditableEntity
{
    public int EnrollmentId { get; private set; }
    public Enrollment Enrollment { get; private set; } = null!;

    public string StripePaymentIntentId { get; private set; } = null!;
    public string? StripeChargeId { get; private set; }
    public string? StripeEventId { get; private set; }
    public long AmountChargedCents { get; private set; }
    public string Currency { get; private set; } = "bam";
    public PaymentStatus Status { get; private set; }
    public DateTime? PaidAt { get; private set; }
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
    {
        EnrollmentId = enrollmentId;
        StripePaymentIntentId = stripePaymentIntentId;
        AmountChargedCents = amountChargedCents;
        Currency = currency;
        Status = status;
    }

    public void MarkSucceeded(
        string stripeChargeId,
        string stripeEventId,
        DateTime paidAt,
        DateTime periodStart,
        DateTime periodEnd)
    {
        StripeChargeId = stripeChargeId;
        StripeEventId = stripeEventId;
        Status = PaymentStatus.Succeeded;
        PaidAt = paidAt;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
    }

    public void MarkFailed(string stripeEventId)
    {
        StripeEventId = stripeEventId;
        Status = PaymentStatus.Failed;
    }
}
