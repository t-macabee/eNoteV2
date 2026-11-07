namespace eNote.Application.Features.Rentals.Instruments;

public class InstrumentRequest
{
    public string Model { get; set; } = null!;
    public int ManufacturerId { get; set; }
    public string? Description { get; set; }

    public int InstrumentTypeId { get; set; }
}
