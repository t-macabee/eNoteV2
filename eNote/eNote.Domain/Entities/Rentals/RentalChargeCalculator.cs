using eNote.Domain.Enums;

namespace eNote.Domain.Entities.Rentals;

public static class RentalChargeCalculator
{
    public const int DaysPerBillingCycle = 30;

    public static RentalCharges Calculate(DateTime? pickedUpAt, DateTime? returnedAt, InstrumentRentalStatus status, decimal fee, DateTime now)
    {
        if (!pickedUpAt.HasValue || !status.IsBillingEligible())
        {
            return new RentalCharges(null, null, null);
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

        return new RentalCharges(DaysCharged: daysCharged, DailyFee: decimal.Round(dailyFee, 2), TotalFee: decimal.Round(daysCharged * dailyFee, 2));
    }
}
