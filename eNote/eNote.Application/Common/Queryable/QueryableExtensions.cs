using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Common.Queryable
{
    public static class QueryableExtensions
    {
        public static IQueryable<InstrumentRental> WithRentalDetails(this IQueryable<InstrumentRental> query)
        {
            return query
                .Include(s => s.StudentProfile)
                .Include(r => r.Instrument)
                    .ThenInclude(i => i.InstrumentType)
                .Include(r => r.Instrument)
                    .ThenInclude(i => i.MusicStore);
        }

        public static IQueryable<Instrument> WithInstrumentDetails(this IQueryable<Instrument> query)
        {
            return query
                .Include(x => x.MusicStore)
                .Include(x => x.InstrumentType)
                .Include(x => x.InstrumentRentals);
        }
    }
}
