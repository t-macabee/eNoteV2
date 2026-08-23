namespace eNote.Domain.Entities.Rentals;

public readonly record struct RentalCharges(
    int? MonthsCharged,
    int? DaysCharged,
    decimal? DailyFee,
    decimal? TotalFee,
    bool IsProrated);
