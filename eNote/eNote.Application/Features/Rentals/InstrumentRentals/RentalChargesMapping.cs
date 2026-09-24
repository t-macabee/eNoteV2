namespace eNote.Application.Features.Rentals.InstrumentRentals;

public static class RentalChargesMapping
{
    public static void ApplyCharges(this InstrumentRentalDto dto, InstrumentRental rental, RentalCharges charges)
    {
        dto.Fee = rental.Fee;
        dto.DailyFee = charges.DailyFee;
        dto.DaysCharged = charges.DaysCharged;
        dto.TotalFee = charges.TotalFee;
    }
}
