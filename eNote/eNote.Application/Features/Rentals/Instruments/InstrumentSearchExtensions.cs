namespace eNote.Application.Features.Rentals.Instruments;

public static class InstrumentSearchExtensions
{
    public static IQueryable<Instrument> ApplySearch(this IQueryable<Instrument> query, InstrumentSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            query = query.Where(x => x.Model.Contains(search.Search!) || x.Manufacturer.Contains(search.Search!));
        }

        if (!string.IsNullOrWhiteSpace(search.Model))
        {
            query = query.Where(x => x.Model.Contains(search.Model!));
        }

        if (!string.IsNullOrWhiteSpace(search.Manufacturer))
        {
            query = query.Where(x => x.Manufacturer.Contains(search.Manufacturer!));
        }

        if (search.InstrumentTypeId.HasValue)
        {
            query = query.Where(x => x.InstrumentTypeId == search.InstrumentTypeId.Value);
        }

        if (search.MusicStoreId.HasValue)
        {
            query = query.Where(x => x.MusicStoreId == search.MusicStoreId.Value);
        }

        if (!search.IsAvailable.HasValue)
        {
            return query;
        }

        return search.IsAvailable.Value
            ? query.WhereHasNoBlockingRental()
            : query.WhereHasBlockingRental();
    }
}
