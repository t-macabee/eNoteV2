using eNote.Domain.Entities;
using eNote.Domain.Enums;

namespace eNote.Application.Features.Rentals.Instruments;

public static class InstrumentAvailabilityExtensions
{
    public static IQueryable<Instrument> WhereHasBlockingRental(this IQueryable<Instrument> query) =>
        query.Where(x => x.InstrumentRentals.Any(r => InstrumentRentalStatusSets.Blocking.Contains(r.RentalStatus)));

    public static IQueryable<Instrument> WhereHasNoBlockingRental(this IQueryable<Instrument> query) =>
        query.Where(x => !x.InstrumentRentals.Any(r => InstrumentRentalStatusSets.Blocking.Contains(r.RentalStatus)));

    public static IQueryable<InstrumentRental> WhereBlockingStatus(this IQueryable<InstrumentRental> query) =>
        query.Where(x => InstrumentRentalStatusSets.Blocking.Contains(x.RentalStatus));
}