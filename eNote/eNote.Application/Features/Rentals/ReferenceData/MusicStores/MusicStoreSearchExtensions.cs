using eNote.Domain.Entities;
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public static class MusicStoreSearchExtensions
{
    public static IQueryable<MusicStore> ApplySearch(this IQueryable<MusicStore> query, MusicStoreSearchObject search) => query.WhereContainsIf(search.StoreName, x => x.StoreName.Contains(search.StoreName!));
}