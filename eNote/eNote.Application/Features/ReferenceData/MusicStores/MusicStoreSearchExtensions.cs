using eNote.Application.Common.Search;
using eNote.Domain.Entities;

namespace eNote.Application.Features.ReferenceData.MusicStores;

public static class MusicStoreSearchExtensions
{
    public static IQueryable<MusicStore> ApplySearch(this IQueryable<MusicStore> query, MusicStoreSearchObject search) => query.WhereContainsIf(search.StoreName, x => x.StoreName.Contains(search.StoreName!));
}