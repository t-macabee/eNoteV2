namespace eNote.Application.Features.ReferenceData.InstrumentTypes;

public sealed class InstrumentTypeDto
{
    public int Id { get; init; }
    public string Type { get; init; } = null!;
    public decimal MonthlyFee { get; init; }
}
