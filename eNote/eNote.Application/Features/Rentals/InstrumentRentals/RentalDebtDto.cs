namespace eNote.Application.Features.Rentals.InstrumentRentals;

public sealed class RentalDebtDto
{
    public bool HasUnpaidDebt { get; set; }
    public int? RentalId { get; set; }
}
