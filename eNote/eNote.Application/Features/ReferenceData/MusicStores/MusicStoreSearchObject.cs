using eNote.Application.Common.Search;

namespace eNote.Application.Features.ReferenceData.MusicStores;

public sealed class MusicStoreSearchObject : BaseSearchObject
{
    public string? StoreName { get; set; }
}