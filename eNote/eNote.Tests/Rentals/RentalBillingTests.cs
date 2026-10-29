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
    }

    [Fact]
    public void CalculateCharges_ActiveRental_ChargesDaysSoFar()
    {
        var rental = new InstrumentRental(1, 1, 1, Now.AddDays(-10), null);
        rental.Approve(50m, null, Now.AddDays(-5), 1);
        rental.Pickup(Now.AddDays(-3));

        var charges = rental.CalculateCharges(Now);

        Assert.Equal(5m, charges.TotalFee);
        Assert.Equal(3, charges.DaysCharged);
    }

    [Fact]
    public void CalculateCharges_ReturnedEarly_UsesProratedDailyFee()
    {
        var rental = new InstrumentRental(1, 1, 1, Now.AddDays(-20), null);
        rental.Approve(30m, null, Now.AddDays(-10), 1);
        rental.Pickup(Now.AddDays(-5));
        rental.ReturnEarly(Now.AddDays(-2), null);

        var charges = rental.CalculateCharges(Now);

        Assert.Equal(3, charges.DaysCharged);
        Assert.Equal(1m, charges.DailyFee);
        Assert.Equal(3m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_After89Days_CompleteAndReturnEarlyChargeTheSame()
    {
        var completed = RentalChargeCalculator.Calculate(Now.AddDays(-89), Now, InstrumentRentalStatus.Completed, 45m, Now);
        var returnedEarly = RentalChargeCalculator.Calculate(Now.AddDays(-89), Now, InstrumentRentalStatus.ReturnedEarly, 45m, Now);

        Assert.Equal(89, completed.DaysCharged);
        Assert.Equal(1.5m, completed.DailyFee);
        Assert.Equal(133.50m, completed.TotalFee);

        Assert.Equal(89, returnedEarly.DaysCharged);
        Assert.Equal(1.5m, returnedEarly.DailyFee);
        Assert.Equal(133.50m, returnedEarly.TotalFee);
    }

    [Fact]
    public void CalculateCharges_ReturnedSameMoment_ChargesOneDay()
    {
        var charges = RentalChargeCalculator.Calculate(Now, Now, InstrumentRentalStatus.Completed, 30m, Now);

        Assert.Equal(1, charges.DaysCharged);
        Assert.Equal(1m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_PartialDay_RoundsUpToNextDay()
    {
        var pickedUpAt = Now.AddDays(-1).AddHours(-1);

        var charges = RentalChargeCalculator.Calculate(pickedUpAt, Now, InstrumentRentalStatus.Completed, 30m, Now);

        Assert.Equal(2, charges.DaysCharged);
        Assert.Equal(2m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_FeeNotDivisibleByCycle_RoundsTotalFromUnroundedDailyFee()
    {
        var charges = RentalChargeCalculator.Calculate(Now.AddDays(-10), Now, InstrumentRentalStatus.Completed, 35m, Now);

        Assert.Equal(10, charges.DaysCharged);
        Assert.Equal(1.17m, charges.DailyFee);
        Assert.Equal(11.67m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_HalfCent_RoundsAwayFromZero()
    {
        var charges = RentalChargeCalculator.Calculate(Now, Now, InstrumentRentalStatus.Completed, 10.35m, Now);

        Assert.Equal(1, charges.DaysCharged);
        Assert.Equal(0.35m, charges.DailyFee);
        Assert.Equal(0.35m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_ReturnedBeforePickup_ChargesOneDay()
    {
        var charges = RentalChargeCalculator.Calculate(Now, Now.AddDays(-1), InstrumentRentalStatus.Completed, 30m, Now);

        Assert.Equal(1, charges.DaysCharged);
        Assert.Equal(1m, charges.TotalFee);
    }

    [Theory]
    [InlineData(InstrumentRentalStatus.Pending)]
    [InlineData(InstrumentRentalStatus.Approved)]
    [InlineData(InstrumentRentalStatus.Rejected)]
    [InlineData(InstrumentRentalStatus.Canceled)]
    public void CalculateCharges_NonBillingStatusWithPickupDate_DoesNotCharge(InstrumentRentalStatus status)
    {
        var charges = RentalChargeCalculator.Calculate(Now.AddDays(-5), null, status, 30m, Now);

        Assert.Null(charges.TotalFee);
        Assert.Null(charges.DaysCharged);
    }
}
