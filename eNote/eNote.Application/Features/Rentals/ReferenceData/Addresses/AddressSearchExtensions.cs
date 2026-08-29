namespace eNote.Application.Features.Rentals.ReferenceData.Addresses;

public static class AddressSearchExtensions
{
    public static IQueryable<Address> ApplySearch(this IQueryable<Address> query, AddressSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.City))
        {
            query = query.Where(x => x.City.Name.Contains(search.City!));
        }

        if (!string.IsNullOrWhiteSpace(search.Street))
        {
            query = query.Where(x => x.Street.Contains(search.Street!));
        }

        return query;
    }
}
