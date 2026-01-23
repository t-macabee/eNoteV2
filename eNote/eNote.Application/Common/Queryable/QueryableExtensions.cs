using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Common.Queryable
{
    public static class QueryableExtensions
    {
        public static IQueryable<InstrumentRental> WithRentalDetails(this IQueryable<InstrumentRental> query)
        {
            return query
                .Include(r => r.Instrument)
                    .ThenInclude(i => i.InstrumentType)
                .Include(r => r.Instrument)
                    .ThenInclude(i => i.MusicShop);
        }

        public static IQueryable<Instrument> WithInstrumentDetails(this IQueryable<Instrument> query)
        {
            return query
                .Include(x => x.MusicShop)
                .Include(x => x.InstrumentType)
                .Include(x => x.InstrumentRentals);
        }
    }
}
