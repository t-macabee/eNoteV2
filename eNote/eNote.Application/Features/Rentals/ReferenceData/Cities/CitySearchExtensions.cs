namespace eNote.Application.Features.Rentals.ReferenceData.Cities;

public static class CitySearchExtensions
{
    public static IQueryable<City> ApplySearch(this IQueryable<City> query, CitySearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(x => x.Name.Contains(search.Name!));
        }

        return query;
    }
}
