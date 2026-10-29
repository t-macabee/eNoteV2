namespace eNote.Tests.Rentals;

public sealed class RentalPaymentTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void MarkPaid_SetsIsPaidAndAmounts()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);

        rental.MarkPaid(5000, Now);

        Assert.True(rental.IsPaid);
        Assert.Equal(Now, rental.PaidAt);
        Assert.Equal(50m, rental.AmountPaid);
    }
}
