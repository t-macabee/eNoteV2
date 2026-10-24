using eNote.Domain.Enums;

namespace eNote.Domain.Entities.Rentals;

public sealed class RentalPayment : StripePayment, ITenantScoped
{
    public int InstrumentRentalId { get; private set; }
    public InstrumentRental InstrumentRental { get; private set; } = null!;
    public int MusicStoreId { get; private set; }

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
        : base(stripePaymentIntentId, amountChargedCents, currency, status)
    {
        InstrumentRentalId = instrumentRentalId;
        MusicStoreId = musicStoreId;
    }

    public void MarkSucceeded(string? stripeChargeId, string stripeEventId, DateTime paidAt)
    {
        MarkSucceededCore(stripeChargeId, stripeEventId, paidAt);
    }

    protected override PaymentRefund CreateRefund(string stripeRefundId, long amountCents, DateTime appliedAtUtc)
    {
        return new PaymentRefund(this, stripeRefundId, amountCents, appliedAtUtc);
    }
}
