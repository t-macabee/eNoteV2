using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.InstrumentRentals;

public static class InstrumentRentalSearchExtensions
{
    public static IQueryable<InstrumentRental> ApplySearch(this IQueryable<InstrumentRental> query, InstrumentRentalSearchObject search) =>
        query
            .WhereEqualsIf(search.InstrumentId, x => x.InstrumentId == search.InstrumentId!.Value)
            .WhereEqualsIf(search.RentalStatus, x => x.RentalStatus == search.RentalStatus!.Value);
}