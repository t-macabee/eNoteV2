namespace eNote.Domain.Entities.Rentals;

public readonly record struct RentalCharges(
    int? DaysCharged,
    decimal? DailyFee,
    decimal? TotalFee);
