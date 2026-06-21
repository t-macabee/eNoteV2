using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Instruments;

public static class InstrumentQueryableExtensions
{
    public static IQueryable<Instrument> WithInstrumentDetails(this IQueryable<Instrument> query) =>
        query
            .Include(x => x.MusicStore)
            .Include(x => x.InstrumentType)
            .Include(x => x.InstrumentRentals);
}