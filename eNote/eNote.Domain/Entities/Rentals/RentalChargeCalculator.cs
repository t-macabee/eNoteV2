using eNote.Domain.Enums;

namespace eNote.Domain.Entities.Rentals;

public static class RentalChargeCalculator
{
    public const int DaysPerBillingCycle = 30;

    /// <summary>
    /// Computes rental charges based on pickup date, return date, status, fee, and evaluation time.
    /// Nothing is billed before pickup or when the status is not billing-eligible.
    /// An early return is prorated by day (fee / 30, capped at the monthly fee);
    /// every other billable status charges whole months (ceiling of days / 30, minimum 1).
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

        if (status == InstrumentRentalStatus.ReturnedEarly)
        {
            var dailyFee = fee / DaysPerBillingCycle;
            var prorated = daysCharged * dailyFee;
            var totalFee = prorated > fee ? fee : prorated;

            return new RentalCharges(MonthsCharged: null, DaysCharged: daysCharged, DailyFee: decimal.Round(dailyFee, 2), TotalFee: decimal.Round(totalFee, 2), IsProrated: true);
        }

        var monthsCharged = (int)Math.Ceiling((end - start).TotalDays / DaysPerBillingCycle);

        if (monthsCharged < 1)
        {
            monthsCharged = 1;
        }

        return new RentalCharges(MonthsCharged: monthsCharged, DaysCharged: null, DailyFee: null, TotalFee: monthsCharged * fee, IsProrated: false);
    }
}
