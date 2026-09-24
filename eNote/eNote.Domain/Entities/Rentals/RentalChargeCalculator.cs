using eNote.Domain.Enums;

namespace eNote.Domain.Entities.Rentals;

public static class RentalChargeCalculator
{
    public const int DaysPerBillingCycle = 30;

    /// <summary>
    /// Computes rental charges based on pickup date, return date, status, fee, and evaluation time.
    /// Nothing is billed before pickup or when the status is not billing-eligible.
    /// Every billable status is charged by the day: days used (ceiling, minimum 1) × fee / 30.
    /// </summary>
    public static RentalCharges Calculate(DateTime? pickedUpAt, DateTime? returnedAt, InstrumentRentalStatus status, decimal fee, DateTime now)
    {
        if (!pickedUpAt.HasValue || !status.IsBillingEligible())
        {
            return new RentalCharges(null, null, null, null, false);
        }

        var start = pickedUpAt.Value;
        var end = returnedAt ?? now;

        if (end < start)
        {
            end = start;
        }

        var daysCharged = (int)Math.Ceiling((end - start).TotalDays);

        if (daysCharged < 1)
        {
            daysCharged = 1;
        }

        var dailyFee = fee / DaysPerBillingCycle;

        return new RentalCharges(MonthsCharged: null, DaysCharged: daysCharged, DailyFee: decimal.Round(dailyFee, 2), TotalFee: decimal.Round(daysCharged * dailyFee, 2), IsProrated: true);
    }
}
