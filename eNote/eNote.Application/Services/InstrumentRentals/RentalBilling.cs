using eNote.Application.DTOs;
using eNote.Domain.Entities;
using eNote.Domain.Enums;

namespace eNote.Application.Services.InstrumentRentals
{
    public static class RentalBilling
    {
        public readonly record struct BillingResult(int? MonthsCharged, decimal? TotalFee);

        public static BillingResult Calculate(InstrumentRental rental, DateTime nowUtc)
        { 
            if(!rental.PickedUpAt.HasValue)
                return new BillingResult(null, null);

            if(rental.RentalStatus is not (InstrumentRentalStatus.Active or InstrumentRentalStatus.Completed))
                return new BillingResult(null, null);

            var start = rental.PickedUpAt.Value;
            var end = rental.ReturnedAt ?? nowUtc;

            if(end < start) end = start;

            var months = (int)Math.Ceiling((end - start).TotalDays / 30.0);

            if(months < 1) months = 1;            

            return new BillingResult(months, months * rental.Fee);
        }

        public static void ApplyBilling(InstrumentRental rental, InstrumentRentalDto dto, DateTime nowUtc)
        {
            dto.MonthlyFee = rental.Fee;

            var result = Calculate(rental, nowUtc);

            dto.MonthsCharged = result.MonthsCharged;

            dto.TotalFee = result.TotalFee;
        }
    }
}
