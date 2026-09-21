namespace eNote.Domain.Entities.Rentals;

public sealed class RentalRefund
{
    public int Id { get; private set; }
    public int RentalPaymentId { get; private set; }
    public RentalPayment RentalPayment { get; private set; } = null!;
    public string StripeRefundId { get; private set; } = null!;
    public long AmountCents { get; private set; }
    public DateTime AppliedAtUtc { get; private set; }

    private RentalRefund()
    {
    }

    public RentalRefund(int rentalPaymentId, string stripeRefundId, long amountCents, DateTime appliedAtUtc)
    {
        RentalPaymentId = rentalPaymentId;
        StripeRefundId = stripeRefundId;
        AmountCents = amountCents;
        AppliedAtUtc = appliedAtUtc;
    }

    public RentalRefund(RentalPayment rentalPayment, string stripeRefundId, long amountCents, DateTime appliedAtUtc)
    {
        RentalPayment = rentalPayment;
        StripeRefundId = stripeRefundId;
        AmountCents = amountCents;
        AppliedAtUtc = appliedAtUtc;
    }
}
