using eNote.Domain.Entities;
using eNote.Domain.Enums;

namespace eNote.Application.Features.Instruments;

public static class InstrumentSearchExtensions
{
    public static IQueryable<Instrument> ApplySearch(this IQueryable<Instrument> query, InstrumentSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Model))
        {
            query = query.Where(x => x.Model.Contains(search.Model));
        }

        if (!string.IsNullOrWhiteSpace(search.Manufacturer))
        {
            query = query.Where(x => x.Manufacturer.Contains(search.Manufacturer));
        }

        if (search.InstrumentTypeId.HasValue)
        {
            query = query.Where(x => x.InstrumentTypeId == search.InstrumentTypeId);
        }

        if (search.IsAvailable.HasValue)
        {
            query = search.IsAvailable.Value
                ? query.Where(x => !x.InstrumentRentals.Any(r =>
                    r.RentalStatus == InstrumentRentalStatus.Approved ||
                    r.RentalStatus == InstrumentRentalStatus.Active))
                : query.Where(x => x.InstrumentRentals.Any(r =>
                    r.RentalStatus == InstrumentRentalStatus.Approved ||
                    r.RentalStatus == InstrumentRentalStatus.Active));
        }

        return query;
    }
}
