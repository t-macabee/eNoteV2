namespace eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;

public static class InstrumentTypeSearchExtensions
{
    public static IQueryable<InstrumentType> ApplySearch(this IQueryable<InstrumentType> query, InstrumentTypeSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Type))
        {
            query = query.Where(x => x.Type.Contains(search.Type!));
        }

        return query;
    }
}
