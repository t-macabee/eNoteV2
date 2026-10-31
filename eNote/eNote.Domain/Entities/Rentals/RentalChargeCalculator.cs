using eNote.Domain.Enums;

namespace eNote.Domain.Entities.Rentals;

public static class RentalChargeCalculator
{
    public const int DaysPerBillingCycle = 30;

    public static RentalCharges Calculate(DateTime? pickedUpAt, DateTime? returnedAt, DateTime? dueAt, InstrumentRentalStatus status, decimal fee, decimal overduePenaltyMultiplier, DateTime now)
    {
        if (!pickedUpAt.HasValue || !status.IsBillingEligible())
        {
            return new RentalCharges(null, null, null, null, null);
        }

        var start = pickedUpAt.Value;
        var end = returnedAt ?? now;

        var daysCharged = (int)Math.Ceiling((end - start).TotalDays);

        if (daysCharged < 1)
        {
            daysCharged = 1;
        }

        var dailyFee = fee / DaysPerBillingCycle;
        var overdueDays = dueAt.HasValue && end > dueAt.Value ? (int)Math.Ceiling((end - dueAt.Value).TotalDays) : 0;
        var baseFee = decimal.Round(daysCharged * dailyFee, 2, MidpointRounding.AwayFromZero);
        var penaltyFee = decimal.Round(overdueDays * dailyFee * overduePenaltyMultiplier, 2, MidpointRounding.AwayFromZero);

        return new RentalCharges(
            DaysCharged: daysCharged,
            DailyFee: decimal.Round(dailyFee, 2, MidpointRounding.AwayFromZero),
            OverdueDays: overdueDays,
            PenaltyFee: penaltyFee,
            TotalFee: baseFee + penaltyFee);
    }

    public static decimal DailyPenaltyFee(decimal fee, decimal overduePenaltyMultiplier) =>
        decimal.Round(fee / DaysPerBillingCycle * overduePenaltyMultiplier, 2, MidpointRounding.AwayFromZero);
}
