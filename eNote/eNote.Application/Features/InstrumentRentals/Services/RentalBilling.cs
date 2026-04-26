using eNote.Application.Features.InstrumentRentals.DTOs;
using eNote.Domain.Entities;
using eNote.Domain.Enums;

namespace eNote.Application.Features.InstrumentRentals.Services
{
    public static class RentalBilling
    {
        public readonly record struct BillingResult(int? MonthsCharged, int? DaysCharged, decimal? DailyFee, decimal? TotalFee, bool IsProrated);

        public static BillingResult Calculate(InstrumentRental rental, DateTime nowUtc)
        { 
            if(!rental.PickedUpAt.HasValue)
                return new BillingResult(null, null, null, null, false);

            if (rental.RentalStatus is not (InstrumentRentalStatus.Active or InstrumentRentalStatus.Completed or InstrumentRentalStatus.ReturnedEarly))
            {
                return new BillingResult(null, null, null, null, false);
            }

            var start = rental.PickedUpAt.Value;
            var end = rental.ReturnedAt ?? nowUtc;

            if(end < start) 
                end = start;

            var totalDays = (end - start).TotalDays;
            var daysCharged = (int)Math.Ceiling(totalDays);

            if(daysCharged < 1)
                daysCharged = 1;

            var monthlyFee = rental.Fee;

            if (rental.RentalStatus == InstrumentRentalStatus.ReturnedEarly)
            {
                var dailyFee = monthlyFee / 30m;
                var prorated = daysCharged * dailyFee;
                var totalFee = prorated > monthlyFee ? monthlyFee : prorated;

                return new BillingResult(
                    MonthsCharged: null, DaysCharged: daysCharged, DailyFee: decimal.Round(dailyFee, 2), TotalFee: decimal.Round(totalFee, 2), IsProrated: true
                );
            }

            var monthsCharged = (int)Math.Ceiling((end - start).TotalDays / 30.0);

            if(monthsCharged < 1) 
                monthsCharged = 1;

            return new BillingResult(
                MonthsCharged: monthsCharged, DaysCharged: null, DailyFee: null, TotalFee: monthsCharged * monthlyFee, IsProrated: false
            );
        }

        public static void ApplyBilling(InstrumentRental rental, InstrumentRentalDto dto, DateTime nowUtc)
        {
            dto.Fee = rental.Fee;

            var result = Calculate(rental, nowUtc);

            dto.MonthsCharged = result.MonthsCharged;
            dto.DaysCharged = result.DaysCharged;
            dto.DailyFee = result.DailyFee;
            dto.IsProrated = result.IsProrated;
            dto.TotalFee = result.TotalFee;
        }

        public static void ApplyBillingFromDto(InstrumentRentalDto dto, DateTime nowUtc)
        {
            if (!dto.PickedUpAt.HasValue)
            {
                dto.MonthsCharged = null;
                dto.DaysCharged = null;
                dto.DailyFee = null;
                dto.IsProrated = false;
                dto.TotalFee = null;
                return;
            }

            if (dto.RentalStatus is not (InstrumentRentalStatus.Active or InstrumentRentalStatus.Completed or InstrumentRentalStatus.ReturnedEarly))
            {
                dto.MonthsCharged = null;
                dto.DaysCharged = null;
                dto.DailyFee = null;
                dto.IsProrated = false;
                dto.TotalFee = null;
                return;
            }

            var start = dto.PickedUpAt.Value;
            var end = dto.ReturnedAt ?? nowUtc;

            if (end < start)
                end = start;

            var totalDays = (end - start).TotalDays;
            var daysCharged = (int)Math.Ceiling(totalDays);

            if (daysCharged < 1)
                daysCharged = 1;

            var monthlyFee = dto.Fee;

            if (dto.RentalStatus == InstrumentRentalStatus.ReturnedEarly)
            {
                var dailyFee = monthlyFee / 30m;
                var prorated = daysCharged * dailyFee;
                var totalFee = prorated > monthlyFee ? monthlyFee : prorated;

                dto.MonthsCharged = null;
                dto.DaysCharged = daysCharged;
                dto.DailyFee = decimal.Round(dailyFee, 2);
                dto.IsProrated = true;
                dto.TotalFee = decimal.Round(totalFee, 2);
                return;
            }

            var monthsCharged = (int)Math.Ceiling((end - start).TotalDays / 30.0);

            if (monthsCharged < 1)
                monthsCharged = 1;

            dto.MonthsCharged = monthsCharged;
            dto.DaysCharged = null;
            dto.DailyFee = null;
            dto.IsProrated = false;
            dto.TotalFee = monthsCharged * monthlyFee;
        }
    }
}
