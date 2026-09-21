namespace eNote.Tests.Rentals;

public sealed class RentalBillingTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CalculateCharges_BeforePickup_DoesNotCharge()
    {
        var rental = new InstrumentRental(1, 1, 1, Now.AddDays(-2), null);

        var charges = rental.CalculateCharges(Now);

        Assert.Null(charges.TotalFee);
        Assert.Null(charges.MonthsCharged);
        Assert.False(charges.IsProrated);
    }

    [Fact]
    public void CalculateCharges_ActiveRental_ChargesAtLeastOneMonth()
    {
        var rental = new InstrumentRental(1, 1, 1, Now.AddDays(-10), null);
        rental.Approve(50m, null, Now.AddDays(-5), 1);
        rental.Pickup(Now.AddDays(-3));

        var charges = rental.CalculateCharges(Now);

        Assert.Equal(50m, charges.TotalFee);
        Assert.Equal(1, charges.MonthsCharged);
        Assert.False(charges.IsProrated);
    }

    [Fact]
    public void CalculateCharges_ReturnedEarly_UsesProratedDailyFee()
    {
        var rental = new InstrumentRental(1, 1, 1, Now.AddDays(-20), null);
        rental.Approve(30m, null, Now.AddDays(-10), 1);
        rental.Pickup(Now.AddDays(-5));
        rental.ReturnEarly(Now.AddDays(-2), null);

        var charges = rental.CalculateCharges(Now);

        Assert.True(charges.IsProrated);
        Assert.Equal(3, charges.DaysCharged);
        Assert.Equal(1m, charges.DailyFee);
        Assert.Equal(3m, charges.TotalFee);
    }

    [Fact]
    public void RentalChargeCalculator_Calculate_DirectInvocationMatchesCalculations()
    {
        var pickedUp = Now.AddDays(-45);
        var activeCharges = RentalChargeCalculator.Calculate(pickedUp, null, InstrumentRentalStatus.Active, 50m, Now);
        Assert.Equal(2, activeCharges.MonthsCharged);
        Assert.Equal(100m, activeCharges.TotalFee);
        Assert.False(activeCharges.IsProrated);

        var earlyCharges = RentalChargeCalculator.Calculate(Now.AddDays(-10), Now.AddDays(-5), InstrumentRentalStatus.ReturnedEarly, 30m, Now);
        Assert.True(earlyCharges.IsProrated);
        Assert.Equal(5, earlyCharges.DaysCharged);
        Assert.Equal(1m, earlyCharges.DailyFee);
        Assert.Equal(5m, earlyCharges.TotalFee);

        var ineligibleCharges = RentalChargeCalculator.Calculate(null, null, InstrumentRentalStatus.Pending, 30m, Now);
        Assert.Null(ineligibleCharges.TotalFee);
    }
}
