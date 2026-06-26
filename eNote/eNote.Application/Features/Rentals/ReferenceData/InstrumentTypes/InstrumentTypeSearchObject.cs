using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;

public sealed class InstrumentTypeSearchObject : BaseSearchObject
{
    public string? Type { get; set; }
}