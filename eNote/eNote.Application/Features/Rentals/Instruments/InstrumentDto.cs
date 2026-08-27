namespace eNote.Application.Features.Rentals.Instruments;

public class InstrumentDto
{
    public int Id { get; set; }

    public string Model { get; set; } = null!;
    public string Manufacturer { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public int InstrumentTypeId { get; set; }
    public string InstrumentType { get; set; } = null!;
    public string MusicStore { get; set; } = null!;

    public bool IsAvailable { get; set; }
}
