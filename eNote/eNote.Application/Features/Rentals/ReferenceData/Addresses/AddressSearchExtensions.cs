using eNote.Application.Common.Search;
using eNote.Domain.Entities.Shared;

namespace eNote.Application.Features.Rentals.ReferenceData.Addresses;

public static class AddressSearchExtensions
{
    public static IQueryable<Address> ApplySearch(this IQueryable<Address> query, AddressSearchObject search) => query
            .WhereContainsIf(search.City, x => x.City.Contains(search.City!))
            .WhereContainsIf(search.Street, x => x.Street.Contains(search.Street!));
}