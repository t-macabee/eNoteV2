using eNote.Application.Common.Search;
using eNote.Domain.Entities;

namespace eNote.Application.Features.InstrumentRentals;

public static class InstrumentRentalSearchExtensions
{
    public static IQueryable<InstrumentRental> ApplySearch(this IQueryable<InstrumentRental> query, InstrumentRentalSearchObject search) =>
        query
            .WhereEqualsIf(search.InstrumentId, x => x.InstrumentId == search.InstrumentId!.Value)
            .WhereEqualsIf(search.RentalStatus, x => x.RentalStatus == search.RentalStatus!.Value);
}