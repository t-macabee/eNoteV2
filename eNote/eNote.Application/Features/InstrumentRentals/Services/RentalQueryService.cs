using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Queryable;
using eNote.Application.Common.Services;
using eNote.Application.Common.Time;
using eNote.Application.Features.InstrumentRentals.DTOs;
using eNote.Application.Features.InstrumentRentals.Search;
using eNote.Application.Features.InstrumentRentals.Services.Interfaces;
using eNote.Application.Features.MusicStores.Context.Services;
using eNote.Domain.Entities;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.InstrumentRentals.Services
{
    public class RentalQueryService(IAppDbContext context, IMapper mapper, IClock clock, IMusicStoreContextService storeContext)
        : BaseService<InstrumentRentalDto, InstrumentRentalSearchObject, InstrumentRental>(context, mapper), IRentalQueryService
    {
        private readonly IClock _clock = clock;
        private readonly IMusicStoreContextService _storeContext = storeContext;

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
            var dto = await _context.Set<InstrumentRental>()
                .AsNoTracking()
                .Where(x => x.Id == rentalId && x.StudentProfile.AppUserId == userId)
                .ProjectToType<InstrumentRentalDto>()
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("ID nije pronađen");

            RentalBilling.ApplyBillingFromDto(dto, _clock.UtcNow);

            return dto;
        }

        public async Task<InstrumentRentalDto> GetByIdForStoreAsync(int rentalId, int userId)
        {
            var storeId = await _storeContext.GetActiveStoreAsync(userId);

            var dto = await _context.Set<InstrumentRental>()
                .AsNoTracking()
                .Where(x => x.Id == rentalId && x.Instrument.MusicStoreId == storeId)
                .ProjectToType<InstrumentRentalDto>()
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("ID nije pronađen");

            RentalBilling.ApplyBillingFromDto(dto, _clock.UtcNow);

            return dto;
        }

        public async Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(int userId, InstrumentRentalSearchObject searchObject)
        {
            var query = _context.Set<InstrumentRental>()
                .AsNoTracking()
                .Where(x => x.StudentProfile.AppUserId == userId);

            query = AddFilter(searchObject, query);

            var projected = query
                .OrderByDescending(x => x.RequestedAt)
                .ProjectToType<InstrumentRentalDto>();

            var paged = await projected
                .ToPagedResultAsync(
                    searchObject.Page,
                    searchObject.PageSize,
                    searchObject.IncludeTotalCount,
                    dto => dto
                );

            foreach (var item in paged.Items)
                RentalBilling.ApplyBillingFromDto(item, _clock.UtcNow);

            return paged;
        }

        public async Task<PagedResult<InstrumentRentalDto>> GetPagedForStoreAsync(int userId, InstrumentRentalSearchObject searchObject)
        {
            var storeId = await _storeContext.GetActiveStoreAsync(userId);

            var query = _context.Set<InstrumentRental>()
                .AsNoTracking()
                .Where(x => x.Instrument.MusicStoreId == storeId);

            query = AddFilter(searchObject, query);

            var projected = query
                .OrderByDescending(x => x.RequestedAt)
                .ProjectToType<InstrumentRentalDto>();

            var paged = await projected
                .ToPagedResultAsync(
                    searchObject.Page,
                    searchObject.PageSize,
                    searchObject.IncludeTotalCount,
                    dto => dto
                );

            foreach (var item in paged.Items)
                RentalBilling.ApplyBillingFromDto(item, _clock.UtcNow);

            return paged;
        }        
    }    
}
