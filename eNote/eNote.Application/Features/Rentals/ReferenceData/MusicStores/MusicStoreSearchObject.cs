using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public sealed class MusicStoreSearchObject : BaseSearchObject
{
    public string? StoreName { get; set; }
    public int? CityId { get; set; }
}