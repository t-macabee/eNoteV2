namespace eNote.Tests.Rentals;

/// <summary>
/// Pure domain tests for payment/refund math and the paid flag. These have no DB
/// and no Stripe dependency; they lock in the billing decision that a refund does
/// not flip InstrumentRental.IsPaid back to false.
/// </summary>
public sealed class RentalPaymentTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Complete_Refund_WholeAmount_LeavesZeroOwed()
    {
        var rental = new InstrumentRental(1, 1, 1, Now.AddDays(-20), null);
        rental.Approve(50m, null, Now.AddDays(-10), 1);
        rental.Pickup(Now.AddDays(-10));
        rental.Complete(Now.AddDays(-1), null);

        var charges = rental.CalculateCharges(rental.ReturnedAt!.Value);
        var cents = (long)Math.Round(charges.TotalFee!.Value * 100m, MidpointRounding.AwayFromZero);
        var payment = new RentalPayment(rental.Id, rental.MusicStoreId, "pi_test_1", cents, "eur", PaymentStatus.Succeeded);

        payment.ApplyRefund(cents, "re_test_1", Now);

        Assert.Equal(0, payment.AmountChargedCents - payment.RefundedCents);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
    }

    [Fact]
    public void ReturnEarly_Refund_CappedAtMonthlyFee()
    {
        var rental = new InstrumentRental(1, 1, 1, Now.AddDays(-20), null);
        rental.Approve(50m, null, Now.AddDays(-10), 1);
        rental.Pickup(Now.AddDays(-5));
        rental.ReturnEarly(Now.AddDays(-2), null);

        var charges = rental.CalculateCharges(rental.ReturnedAt!.Value);
        var cents = (long)Math.Round(charges.TotalFee!.Value * 100m, MidpointRounding.AwayFromZero);
        var payment = new RentalPayment(rental.Id, rental.MusicStoreId, "pi_test_1", cents, "eur", PaymentStatus.Succeeded);

        payment.ApplyRefund(cents, "re_test_1", Now);

        Assert.True(payment.RefundedCents <= rental.Fee * 100m);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
    }

    [Fact]
    public void MarkPaid_SetsIsPaidAndAmounts()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);

        rental.MarkPaid(5000, Now);

        Assert.True(rental.IsPaid);
        Assert.Equal(Now, rental.PaidAt);
        Assert.Equal(50m, rental.AmountPaid);
    }

    // Regression: a naive implementation would flip IsPaid back to false when a
    // refund is issued. The product decision is that once paid, the rental stays
    // paid; refund state lives entirely on RentalPayment (there is no MarkRefunded
    // on InstrumentRental by design).
    [Fact]
    public void Refund_Succeeds_IsPaidRemainsTrue()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        rental.MarkPaid(5000, Now);
        var payment = new RentalPayment(rental.Id, rental.MusicStoreId, "pi_test_1", 5000, "eur", PaymentStatus.Succeeded);

        payment.ApplyRefund(5000, "re_test_1", Now);

        Assert.True(rental.IsPaid);
        Assert.Equal(Now, rental.PaidAt);
        Assert.Equal(50m, rental.AmountPaid);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
    }

    [Fact]
    public void ApplyRefund_Partial_LeavesPartiallyRefunded()
    {
        var payment = new RentalPayment(1, 1, "pi_test_1", 5000, "eur", PaymentStatus.Succeeded);

        payment.ApplyRefund(2000, "re_test_1", Now);

        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(2000, payment.RefundedCents);
        Assert.NotNull(payment.RefundedAt);
    }

    [Fact]
    public void ApplyRefund_SecondPartial_AccumulatesToRefunded()
    {
        var payment = new RentalPayment(1, 1, "pi_test_1", 5000, "eur", PaymentStatus.Succeeded);

        payment.ApplyRefund(2000, "re_test_1", Now);
        payment.ApplyRefund(3000, "re_test_2", Now);

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(5000, payment.RefundedCents);
    }
}
