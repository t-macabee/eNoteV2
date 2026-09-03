using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.Instruments;

public class InstrumentSearchObject : BaseSearchObject
{
    public string? Search { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public int? InstrumentTypeId { get; set; }
    public int? MusicStoreId { get; set; }

    public bool? IsAvailable { get; set; }
}

