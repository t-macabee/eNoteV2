using eNote.Application.DTOs;
using eNote.Application.Interfaces;
using eNote.Application.Interfaces.Instruments.InstrumentRentals;
using eNote.Application.Interfaces.Ports;
using eNote.Application.SearchObjects;
using eNote.Application.Services.Base;
using eNote.Application.Services.Instruments.Rentals;
using eNote.Domain.Entities;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Services.Instruments.Rentals
{
    public class RentalQueryService(IAppDbContext context, IMapper mapper) : BaseService<InstrumentRentalDto, InstrumentRentalSearchObject, InstrumentRental>(context, mapper), IRentalQueryService
    {
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

            RentalBilling.ApplyBilling(entity, result, DateTime.UtcNow);

            return result;
        }       

        public async Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId, int userId)
        {
            var query = _context.Set<InstrumentRental>().AsNoTracking();

            query = query.WithRentalDetails();

            var entity = await query.FirstOrDefaultAsync(x => x.Id == rentalId && x.StudentId == userId)
                ?? throw new KeyNotFoundException("ID nije pronađen");

            return MapEntityToModel(entity);
        }

        public async Task<InstrumentRentalDto> GetByIdForShopAsync(int rentalId, int userId)
        {
            var query = _context.Set<InstrumentRental>().AsNoTracking();

            query = query.WithRentalDetails();

            var entity = await query.FirstOrDefaultAsync(x => x.Id == rentalId && x.Instrument.MusicShopId == userId)
                ?? throw new KeyNotFoundException("ID nije pronađen");

            return MapEntityToModel(entity);
        }

        public async Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(int userId, InstrumentRentalSearchObject searchObject)
        {
            var query = _context.Set<InstrumentRental>().AsNoTracking().AsQueryable();

            query = query.WithRentalDetails();

            query = query.Where(x => x.StudentId == userId);

            query = AddFilter(searchObject, query);

            return await PagedResultAsync(searchObject, query);
        }

        public async Task<PagedResult<InstrumentRentalDto>> GetPagedForShopAsync(int userId, InstrumentRentalSearchObject searchObject)
        {
            var query = _context.Set<InstrumentRental>().AsNoTracking().AsQueryable();

            query = query.WithRentalDetails();

            query = query.Where(x => x.Instrument.MusicShopId == userId);

            query = AddFilter(searchObject, query);

            return await PagedResultAsync(searchObject, query);
        }

        private async Task<PagedResult<InstrumentRentalDto>> PagedResultAsync(InstrumentRentalSearchObject search, IQueryable<InstrumentRental> query)
        {
            int? totalCount = null;

            if (search.IncludeTotalCount)
                totalCount = await query.CountAsync();

            var page = search.Page < 1 ? 1 : search.Page;
            var pageSize = search.PageSize < 1 ? 20 : search.PageSize;

            var entities = await query
                .OrderByDescending(r => r.RequestedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = entities.Select(MapEntityToModel).ToList();

            return new PagedResult<InstrumentRentalDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }        
    }    
}
