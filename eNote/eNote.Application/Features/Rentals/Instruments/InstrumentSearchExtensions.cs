using eNote.Application.Common.Search;
using eNote.Domain.Entities.Rentals;

namespace eNote.Application.Features.Rentals.Instruments;

public static class InstrumentSearchExtensions
{
    public static IQueryable<Instrument> ApplySearch(this IQueryable<Instrument> query, InstrumentSearchObject search)
    {
        query = query
            .WhereContainsIf(search.Model, x => x.Model.Contains(search.Model!))
            .WhereContainsIf(search.Manufacturer, x => x.Manufacturer.Contains(search.Manufacturer!))
            .WhereEqualsIf(search.InstrumentTypeId, x => x.InstrumentTypeId == search.InstrumentTypeId!.Value);

        if (!search.IsAvailable.HasValue)
        {
            return query;
        }

        return search.IsAvailable.Value
            ? query.WhereHasNoBlockingRental()
            : query.WhereHasBlockingRental();
    }
}