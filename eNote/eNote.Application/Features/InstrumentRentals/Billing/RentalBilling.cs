using eNote.Domain.Entities;
using eNote.Domain.Enums;

namespace eNote.Application.Features.InstrumentRentals.Billing
{
    public static class RentalBilling
    {
        public static void ApplyBilling(InstrumentRental rental, InstrumentRentalDto dto, DateTime nowUtc)
        {
            dto.Fee = rental.Fee;

            var result = Calculate(rental.Fee, rental.PickedUpAt, rental.ReturnedAt, rental.RentalStatus, nowUtc);

            dto.MonthsCharged = result.MonthsCharged;
            dto.DaysCharged = result.DaysCharged;
            dto.DailyFee = result.DailyFee;
            dto.IsProrated = result.IsProrated;
            dto.TotalFee = result.TotalFee;
        }

        private static BillingResult Calculate(decimal fee, DateTime? pickedUpAt, DateTime? returnedAt, InstrumentRentalStatus status, DateTime nowUtc)
        {
            if (!pickedUpAt.HasValue)
                return new BillingResult(null, null, null, null, false);

            if (status is not (InstrumentRentalStatus.Active or InstrumentRentalStatus.Completed or InstrumentRentalStatus.ReturnedEarly))
                return new BillingResult(null, null, null, null, false);

            var start = pickedUpAt.Value;

            var end = returnedAt ?? nowUtc;

            if (end < start)
                end = start;

            var daysCharged = (int)Math.Ceiling((end - start).TotalDays);

            if (daysCharged < 1)
                daysCharged = 1;

            if (status == InstrumentRentalStatus.ReturnedEarly)
            {
                var dailyFee = fee / 30m;
                var prorated = daysCharged * dailyFee;
                var totalFee = prorated > fee ? fee : prorated;

                return new BillingResult(
                    MonthsCharged: null,
                    DaysCharged: daysCharged,
                    DailyFee: decimal.Round(dailyFee, 2),
                    TotalFee: decimal.Round(totalFee, 2),
                    IsProrated: true
                );
            }

            var monthsCharged = (int)Math.Ceiling((end - start).TotalDays / 30.0);

            if (monthsCharged < 1)
                monthsCharged = 1;

            return new BillingResult(
                MonthsCharged: monthsCharged,
                DaysCharged: null,
                DailyFee: null,
                TotalFee: monthsCharged * fee,
                IsProrated: false
            );
        }

        private readonly record struct BillingResult(int? MonthsCharged, int? DaysCharged, decimal? DailyFee, decimal? TotalFee, bool IsProrated);
    }
}
