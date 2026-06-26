using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.Instruments;

public class InstrumentSearchObject : BaseSearchObject
{
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public int? InstrumentTypeId { get; set; }

    public bool? IsAvailable { get; set; }
}
