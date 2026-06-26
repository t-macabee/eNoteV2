using eNote.Domain.Entities.Rentals;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Rentals.InstrumentRentals;

public static class RentalQueryableExtensions
{
    public static IQueryable<InstrumentRental> WithRentalDetails(this IQueryable<InstrumentRental> query) =>
        query
            .Include(s => s.StudentProfile)
            .Include(r => r.Instrument).ThenInclude(i => i.InstrumentType)
            .Include(r => r.Instrument).ThenInclude(i => i.MusicStore);
}