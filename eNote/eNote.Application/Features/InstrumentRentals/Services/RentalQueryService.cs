using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Queryable;
using eNote.Application.Common.Services;
using eNote.Application.Common.Time;
using eNote.Application.Features.InstrumentRentals.DTOs;
using eNote.Application.Features.InstrumentRentals.Search;
using eNote.Application.Features.InstrumentRentals.Services.Interfaces;
using eNote.Domain.Entities;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.InstrumentRentals.Services
{
    public class RentalQueryService(IAppDbContext context, IMapper mapper, IClock clock)
        : BaseService<InstrumentRentalDto, InstrumentRentalSearchObject, InstrumentRental>(context, mapper), IRentalQueryService
    {
        private readonly IClock _clock = clock;

        protected override IQueryable<InstrumentRental> AddIncludes(IQueryable<InstrumentRental> query) => query.WithRentalDetails();

        protected override IQueryable<InstrumentRental> AddFilter(InstrumentRentalSearchObject search, IQueryable<InstrumentRental> query)
        {
            if (search.InstrumentId.HasValue)
                query = query.Where(x => x.InstrumentId == search.InstrumentId.Value);

            if (search.RentalStatus.HasValue)
                query = query.Where(x => x.RentalStatus == search.RentalStatus.Value);

            return query;
        }

        protected override InstrumentRentalDto MapEntityToModel(InstrumentRental entity)
        {
            var result = _mapper.Map<InstrumentRentalDto>(entity);

            RentalBilling.ApplyBilling(entity, result, _clock.UtcNow);

            return result;
        }       

        public async Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId, int userId)
        {
            var query = _context.Set<InstrumentRental>().AsNoTracking();

            query = query.WithRentalDetails();

            var entity = await query
                .FirstOrDefaultAsync(x => x.Id == rentalId && x.StudentProfile.AppUserId == userId)
                ?? throw new KeyNotFoundException("ID nije pronađen");

            return MapEntityToModel(entity);
        }

        public async Task<InstrumentRentalDto> GetByIdForShopAsync(int rentalId, int userId)
        {
            var query = _context.Set<InstrumentRental>().AsNoTracking();

            query = query.WithRentalDetails();

            var entity = await query
                .FirstOrDefaultAsync(x => x.Id == rentalId && x.Instrument.MusicShop.AppUserId == userId)
                ?? throw new KeyNotFoundException("ID nije pronađen");

            return MapEntityToModel(entity);
        }

        public async Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(int userId, InstrumentRentalSearchObject searchObject)
        {
            var query = _context.Set<InstrumentRental>().AsNoTracking().AsQueryable();

            query = query.WithRentalDetails();

            query = query.Where(x => x.StudentProfile.AppUserId == userId);

            query = AddFilter(searchObject, query);

            return await query
                 .OrderByDescending(x => x.RequestedAt)
                 .ToPagedResultAsync(
                    searchObject.Page, 
                    searchObject.PageSize, 
                    searchObject.IncludeTotalCount, 
                    MapEntityToModel,
                    orderBy: x => x.OrderByDescending(r => r.RequestedAt)                    
                 );
        }

        public async Task<PagedResult<InstrumentRentalDto>> GetPagedForShopAsync(int userId, InstrumentRentalSearchObject searchObject)
        {
            var query = _context.Set<InstrumentRental>().AsNoTracking().AsQueryable();

            query = query.WithRentalDetails();

            query = query.Where(x => x.Instrument.MusicShop.AppUserId == userId);

            query = AddFilter(searchObject, query);

            return await query
                 .OrderByDescending(x => x.RequestedAt)
                 .ToPagedResultAsync(searchObject.Page, searchObject.PageSize, searchObject.IncludeTotalCount, MapEntityToModel);
        }        
    }    
}
