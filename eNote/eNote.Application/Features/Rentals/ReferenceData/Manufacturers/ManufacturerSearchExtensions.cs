namespace eNote.Application.Features.Rentals.ReferenceData.Manufacturers;

public static class ManufacturerSearchExtensions
{
    public static IQueryable<Manufacturer> ApplySearch(this IQueryable<Manufacturer> query, ManufacturerSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(x => x.Name.Contains(search.Name!));
        }

        return query;
    }
}
