namespace eNote.Application.Features.Rentals.Instruments;

public class InstrumentUpdateRequest
{
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public string? Description { get; set; }
    public string? ImagePath { get; set; }

    public int? InstrumentTypeId { get; set; }
}
