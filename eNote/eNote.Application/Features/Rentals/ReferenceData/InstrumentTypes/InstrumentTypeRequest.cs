namespace eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;

public sealed class InstrumentTypeRequest
{
    public string Type { get; set; } = null!;
    public decimal MonthlyFee { get; set; }
}
