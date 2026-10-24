namespace eNote.Domain.Entities.Shared;

public sealed class PaymentRefund
{
    public int Id { get; private set; }
    public int? RentalPaymentId { get; private set; }
    public RentalPayment? RentalPayment { get; private set; }
    public int? CoursePaymentId { get; private set; }
    public CoursePayment? CoursePayment { get; private set; }
    public string StripeRefundId { get; private set; } = null!;
    public long AmountCents { get; private set; }
    public DateTime AppliedAtUtc { get; private set; }

    private PaymentRefund()
    {
    }

    public PaymentRefund(RentalPayment rentalPayment, string stripeRefundId, long amountCents, DateTime appliedAtUtc)
    {
        RentalPayment = rentalPayment;
        StripeRefundId = stripeRefundId;
        AmountCents = amountCents;
        AppliedAtUtc = appliedAtUtc;
    }

    public PaymentRefund(CoursePayment coursePayment, string stripeRefundId, long amountCents, DateTime appliedAtUtc)
    {
        CoursePayment = coursePayment;
        StripeRefundId = stripeRefundId;
        AmountCents = amountCents;
        AppliedAtUtc = appliedAtUtc;
    }
}
