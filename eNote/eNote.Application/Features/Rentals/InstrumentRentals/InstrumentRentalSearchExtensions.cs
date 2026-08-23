namespace eNote.Application.Features.Rentals.InstrumentRentals;

public static class InstrumentRentalSearchExtensions
{
    public static IQueryable<InstrumentRental> ApplySearch(this IQueryable<InstrumentRental> query, InstrumentRentalSearchObject search)
    {
        if (search.InstrumentId.HasValue)
        {
            query = query.Where(x => x.InstrumentId == search.InstrumentId.Value);
        }

        if (search.RentalStatus.HasValue)
        {
            query = query.Where(x => x.RentalStatus == search.RentalStatus.Value);
        }

        return query;
    }
}
