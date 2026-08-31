namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public static class MusicStoreSearchExtensions
{
    public static IQueryable<MusicStore> ApplySearch(this IQueryable<MusicStore> query, MusicStoreSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.StoreName))
        {
            query = query.Where(x => x.StoreName.Contains(search.StoreName!));
        }

        if (search.CityId.HasValue)
        {
            query = query.Where(x => x.Address != null && x.Address.CityId == search.CityId.Value);
        }

        return query;
    }
}
