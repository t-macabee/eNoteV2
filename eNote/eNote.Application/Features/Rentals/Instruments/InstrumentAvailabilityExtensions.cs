using eNote.Domain.Entities;
using eNote.Domain.Entities.Rentals;
using eNote.Domain.Enums;

namespace eNote.Application.Features.Rentals.Instruments;

public static class InstrumentAvailabilityExtensions
{
    public static IQueryable<Instrument> WhereHasBlockingRental(this IQueryable<Instrument> query) =>
        query.Where(x => x.InstrumentRentals.Any(r => r.RentalStatus == InstrumentRentalStatus.Approved || r.RentalStatus == InstrumentRentalStatus.Active));

    public static IQueryable<Instrument> WhereHasNoBlockingRental(this IQueryable<Instrument> query) =>
        query.Where(x => !x.InstrumentRentals.Any(r => r.RentalStatus == InstrumentRentalStatus.Approved || r.RentalStatus == InstrumentRentalStatus.Active));

    public static IQueryable<InstrumentRental> WhereBlockingStatus(this IQueryable<InstrumentRental> query) =>
        query.Where(x => x.RentalStatus == InstrumentRentalStatus.Approved || x.RentalStatus == InstrumentRentalStatus.Active);
}