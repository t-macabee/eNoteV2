namespace eNote.Application.Features.Rentals.Instruments;

public class InstrumentRequest
{
    public string Model { get; set; } = null!;
    public string Manufacturer { get; set; } = null!;
    public string? Description { get; set; }

    public int InstrumentTypeId { get; set; }
}
