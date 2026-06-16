using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.InstrumentRentals.Billing;
using eNote.Application.Features.InstrumentRentals.Search;
using eNote.Application.Features.InstrumentRentals.Services.Interfaces;
using eNote.Application.Features.MusicStores.Services.Interfaces;
using eNote.Domain.Entities;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.InstrumentRentals.Services
{
    public class RentalQueryService(IAppDbContext context, IMapper mapper, IClock clock, IMusicStoreContextService storeContext, ICurrentUserService currentUserService) : IRentalQueryService
    {
        private static IQueryable<InstrumentRental> AddIncludes(IQueryable<InstrumentRental> query) => query.WithRentalDetails();

        private static IQueryable<InstrumentRental> AddFilter(InstrumentRentalSearchObject search, IQueryable<InstrumentRental> query)
        {
            if (search.InstrumentId.HasValue)
                query = query.Where(x => x.InstrumentId == search.InstrumentId.Value);

            if (search.RentalStatus.HasValue)
                query = query.Where(x => x.RentalStatus == search.RentalStatus.Value);

            return query;
        }

        private InstrumentRentalDto MapEntityToModel(InstrumentRental entity)
        {
            var result = mapper.Map<InstrumentRentalDto>(entity);

            RentalBilling.ApplyBilling(entity, result, clock.UtcNow);

            return result;
        }

        public async Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId)
        {
            var entity = await context.Set<InstrumentRental>()
                .AsNoTracking()
                .WithRentalDetails()
                .FirstOrDefaultAsync(x => x.Id == rentalId && x.StudentProfile.AppUserId == currentUserService.UserId)
                ?? throw new NotFoundException(Messages.NotFound);

            return MapEntityToModel(entity);
        }

        public async Task<InstrumentRentalDto> GetByIdForStoreAsync(int rentalId)
        {
            var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

            var entity = await context.Set<InstrumentRental>()
                .AsNoTracking()
                .WithRentalDetails()
                .FirstOrDefaultAsync(x => x.Id == rentalId && x.Instrument.MusicStoreId == storeId)
                ?? throw new NotFoundException(Messages.NotFound);

            return MapEntityToModel(entity);
        }

        public async Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(InstrumentRentalSearchObject searchObject)
        {
            var query = context.Set<InstrumentRental>()
                .AsNoTracking()
                .Where(x => x.StudentProfile.AppUserId == currentUserService.UserId);

            query = AddIncludes(query);

            query = AddFilter(searchObject, query);

            return await query
                .OrderByDescending(x => x.RequestedAt)
                .ToPagedResultAsync(
                    searchObject.Page,
                    searchObject.PageSize,
                    searchObject.IncludeTotalCount,
                    MapEntityToModel
                );
        }

        public async Task<PagedResult<InstrumentRentalDto>> GetPagedForStoreAsync(InstrumentRentalSearchObject searchObject)
        {
            var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

            var query = context.Set<InstrumentRental>()
                .AsNoTracking()
                .Where(x => x.Instrument.MusicStoreId == storeId);

            query = AddIncludes(query);

            query = AddFilter(searchObject, query);

            return await query
                .OrderByDescending(x => x.RequestedAt)
                .ToPagedResultAsync(
                    searchObject.Page,
                    searchObject.PageSize,
                    searchObject.IncludeTotalCount,
                    MapEntityToModel
                );
        }
    }
}