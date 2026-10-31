using eNote.Tests.TestUtils;

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
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Active, Now, approvedAt: Now.AddDays(-5), pickedUpAt: Now.AddDays(-3));

        var charges = rental.CalculateCharges(Now);

        Assert.Equal(5m, charges.TotalFee);
        Assert.Equal(3, charges.DaysCharged);
    }

    [Fact]
    public void CalculateCharges_ReturnedEarly_UsesProratedDailyFee()
    {
        var rental = new InstrumentRental(1, 1, 1, Now.AddDays(-20), null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.ReturnedEarly, Now, fee: 30m, approvedAt: Now.AddDays(-10), pickedUpAt: Now.AddDays(-5), finishedAt: Now.AddDays(-2));

        var charges = rental.CalculateCharges(Now);

        Assert.Equal(3, charges.DaysCharged);
        Assert.Equal(1m, charges.DailyFee);
        Assert.Equal(3m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_After89Days_CompleteAndReturnEarlyChargeTheSame()
    {
        var completed = RentalChargeCalculator.Calculate(Now.AddDays(-89), Now, Now.AddDays(-59), InstrumentRentalStatus.Completed, 45m, 0.5m, Now);
        var returnedEarly = RentalChargeCalculator.Calculate(Now.AddDays(-89), Now, Now.AddDays(-59), InstrumentRentalStatus.ReturnedEarly, 45m, 0.5m, Now);

        Assert.Equal(89, completed.DaysCharged);
        Assert.Equal(1.5m, completed.DailyFee);
        Assert.Equal(177.75m, completed.TotalFee);

        Assert.Equal(89, returnedEarly.DaysCharged);
        Assert.Equal(1.5m, returnedEarly.DailyFee);
        Assert.Equal(177.75m, returnedEarly.TotalFee);
    }

    [Fact]
    public void CalculateCharges_ReturnedSameMoment_ChargesOneDay()
    {
        var charges = RentalChargeCalculator.Calculate(Now, Now, Now.AddDays(30), InstrumentRentalStatus.Completed, 30m, 0.5m, Now);

        Assert.Equal(1, charges.DaysCharged);
        Assert.Equal(1m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_PartialDay_RoundsUpToNextDay()
    {
        var pickedUpAt = Now.AddDays(-1).AddHours(-1);

        var charges = RentalChargeCalculator.Calculate(pickedUpAt, Now, pickedUpAt.AddDays(30), InstrumentRentalStatus.Completed, 30m, 0.5m, Now);

        Assert.Equal(2, charges.DaysCharged);
        Assert.Equal(2m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_FeeNotDivisibleByCycle_RoundsTotalFromUnroundedDailyFee()
    {
        var charges = RentalChargeCalculator.Calculate(Now.AddDays(-10), Now, Now.AddDays(20), InstrumentRentalStatus.Completed, 35m, 0.5m, Now);

        Assert.Equal(10, charges.DaysCharged);
        Assert.Equal(1.17m, charges.DailyFee);
        Assert.Equal(11.67m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_HalfCent_RoundsAwayFromZero()
    {
        var charges = RentalChargeCalculator.Calculate(Now, Now, Now.AddDays(30), InstrumentRentalStatus.Completed, 10.35m, 0.5m, Now);

        Assert.Equal(1, charges.DaysCharged);
        Assert.Equal(0.35m, charges.DailyFee);
        Assert.Equal(0.35m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_ReturnedBeforePickup_ChargesOneDay()
    {
        var charges = RentalChargeCalculator.Calculate(Now, Now.AddDays(-1), Now.AddDays(30), InstrumentRentalStatus.Completed, 30m, 0.5m, Now);

        Assert.Equal(1, charges.DaysCharged);
        Assert.Equal(1m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_ThreeOverdueDays_AddsPenaltyToTotal()
    {
        var pickedUpAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var dueAt = pickedUpAt.AddDays(30);
        var charges = RentalChargeCalculator.Calculate(pickedUpAt, pickedUpAt.AddDays(33), dueAt, InstrumentRentalStatus.Completed, 60m, 0.5m, dueAt.AddDays(3));

        Assert.Equal(33, charges.DaysCharged);
        Assert.Equal(2.00m, charges.DailyFee);
        Assert.Equal(3, charges.OverdueDays);
        Assert.Equal(3.00m, charges.PenaltyFee);
        Assert.Equal(69.00m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_ReturnedExactlyAtDue_HasNoPenalty()
    {
        var pickedUpAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var dueAt = pickedUpAt.AddDays(30);
        var charges = RentalChargeCalculator.Calculate(pickedUpAt, dueAt, dueAt, InstrumentRentalStatus.Completed, 60m, 0.5m, dueAt);

        Assert.Equal(30, charges.DaysCharged);
        Assert.Equal(0, charges.OverdueDays);
        Assert.Equal(0.00m, charges.PenaltyFee);
        Assert.Equal(60.00m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_ReturnedTwoHoursAfterDue_ChargesOneOverdueDay()
    {
        var pickedUpAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var dueAt = pickedUpAt.AddDays(30);
        var returnedAt = dueAt.AddHours(2);
        var charges = RentalChargeCalculator.Calculate(pickedUpAt, returnedAt, dueAt, InstrumentRentalStatus.Completed, 60m, 0.5m, returnedAt);

        Assert.Equal(31, charges.DaysCharged);
        Assert.Equal(1, charges.OverdueDays);
        Assert.Equal(1.00m, charges.PenaltyFee);
        Assert.Equal(63.00m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_ReturnedOneMinuteAfterDue_ChargesOneOverdueDay()
    {
        var pickedUpAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var dueAt = pickedUpAt.AddDays(30);
        var returnedAt = dueAt.AddMinutes(1);
        var charges = RentalChargeCalculator.Calculate(pickedUpAt, returnedAt, dueAt, InstrumentRentalStatus.Completed, 60m, 0.5m, returnedAt);

        Assert.Equal(31, charges.DaysCharged);
        Assert.Equal(1, charges.OverdueDays);
        Assert.Equal(1.00m, charges.PenaltyFee);
        Assert.Equal(63.00m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_HigherMultiplier_ScalesThePenalty()
    {
        var pickedUpAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var dueAt = pickedUpAt.AddDays(30);
        var returnedAt = dueAt.AddDays(2);
        var charges = RentalChargeCalculator.Calculate(pickedUpAt, returnedAt, dueAt, InstrumentRentalStatus.Completed, 35m, 1.5m, returnedAt);

        Assert.Equal(1.17m, charges.DailyFee);
        Assert.Equal(2, charges.OverdueDays);
        Assert.Equal(3.50m, charges.PenaltyFee);
        Assert.Equal(40.83m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_ActiveRental_IncludesPenaltyForDaysSoFar()
    {
        var pickedUpAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var dueAt = pickedUpAt.AddDays(30);
        var now = dueAt.AddDays(4);
        var charges = RentalChargeCalculator.Calculate(pickedUpAt, null, dueAt, InstrumentRentalStatus.Active, 90m, 0.5m, now);

        Assert.Equal(34, charges.DaysCharged);
        Assert.Equal(4, charges.OverdueDays);
        Assert.Equal(6.00m, charges.PenaltyFee);
        Assert.Equal(108.00m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_HalfCentPenalty_RoundsAwayFromZero()
    {
        var pickedUpAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var dueAt = pickedUpAt.AddDays(30);
        var returnedAt = dueAt.AddDays(1);
        var charges = RentalChargeCalculator.Calculate(pickedUpAt, returnedAt, dueAt, InstrumentRentalStatus.Completed, 10.35m, 1m, returnedAt);

        Assert.Equal(0.35m, charges.PenaltyFee);
        Assert.Equal(11.05m, charges.TotalFee);
    }

    [Fact]
    public void CalculateCharges_ActiveWithoutDueAt_HasNoPenalty()
    {
        var pickedUpAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var charges = RentalChargeCalculator.Calculate(pickedUpAt, null, null, InstrumentRentalStatus.Active, 60m, 0.5m, pickedUpAt.AddDays(40));

        Assert.Equal(0, charges.OverdueDays);
        Assert.Equal(0.00m, charges.PenaltyFee);
    }

    [Fact]
    public void CalculateCharges_ApprovedRental_HasNoBillingValues()
    {
        var charges = RentalChargeCalculator.Calculate(Now, Now, Now.AddDays(30), InstrumentRentalStatus.Approved, 60m, 0.5m, Now);

        Assert.Null(charges.DaysCharged);
        Assert.Null(charges.DailyFee);
        Assert.Null(charges.OverdueDays);
        Assert.Null(charges.PenaltyFee);
        Assert.Null(charges.TotalFee);
    }

    [Theory]
    [InlineData(60, 0.75, 1.50)]
    [InlineData(10.35, 1, 0.35)]
    public void DailyPenaltyFee_RoundsAwayFromZero(decimal fee, decimal multiplier, decimal expected)
    {
        Assert.Equal(expected, RentalChargeCalculator.DailyPenaltyFee(fee, multiplier));
    }

    [Theory]
    [InlineData(InstrumentRentalStatus.Pending)]
    [InlineData(InstrumentRentalStatus.Approved)]
    [InlineData(InstrumentRentalStatus.Rejected)]
    [InlineData(InstrumentRentalStatus.Canceled)]
    public void CalculateCharges_NonBillingStatusWithPickupDate_DoesNotCharge(InstrumentRentalStatus status)
    {
        var charges = RentalChargeCalculator.Calculate(Now.AddDays(-5), null, Now.AddDays(25), status, 30m, 0.5m, Now);

        Assert.Null(charges.TotalFee);
        Assert.Null(charges.DaysCharged);
    }
}
