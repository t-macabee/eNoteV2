namespace eNote.Application.Features.Rentals.InstrumentRentals;

public static class RentalQueryableExtensions
{
    public static IQueryable<InstrumentRental> WithRentalDetails(this IQueryable<InstrumentRental> query) =>
        query
            .Include(s => s.StudentProfile)
            .Include(r => r.Instrument).ThenInclude(i => i.InstrumentType)
            .Include(r => r.Instrument).ThenInclude(i => i.MusicStore);

    // Safe audit bypass: removes only the IsActive filter while re-applying tenant isolation.
    public static IQueryable<InstrumentRental> ForStoreAudit(this IQueryable<InstrumentRental> query, int storeId) =>
        query.IgnoreQueryFilters().Where(x => x.MusicStoreId == storeId);
}