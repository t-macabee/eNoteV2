using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Queryable;
using eNote.Application.Features.Instruments.Search;
using eNote.Application.Features.Instruments.Services.Interfaces;
using eNote.Application.Features.MusicStores.Services.Interfaces;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Instruments.Services
{
    public class InstrumentService(IAppDbContext context, IMapper mapper, IMusicStoreContextService storeContext) : IInstrumentService
    {
        private InstrumentDto MapEntityToModel(Instrument entity) => mapper.Map<InstrumentDto>(entity);

        public async Task<InstrumentDto> GetByIdAsync(int id, int employeeAppUserId)
        {
            var storeId = await storeContext.GetActiveStoreAsync(employeeAppUserId);

            var query = AddIncludes(context.Set<Instrument>()
                .AsNoTracking()
                .Where(x => x.MusicStoreId == storeId && x.IsActive));

            var entity = await query
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new NotFoundException(Messages.NotFound);

            return MapEntityToModel(entity);
        }

        public async Task<InstrumentDto> GetPublicByIdAsync(int id)
        {
            var entity = await AddIncludes(context.Set<Instrument>().AsNoTracking())
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive)
                ?? throw new NotFoundException(Messages.NotFound);

            return MapEntityToModel(entity);
        }

        public async Task<PagedResult<InstrumentDto>> GetPagedAsync(InstrumentSearchObject search, int employeeAppUserId)
        {
            var storeId = await storeContext.GetActiveStoreAsync(employeeAppUserId);

            var query = context.Set<Instrument>()
                .AsNoTracking()
                .Where(x => x.MusicStoreId == storeId && x.IsActive);

            query = AddIncludes(query);

            query = AddFilter(search, query);

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, MapEntityToModel);
        }

        public async Task<PagedResult<InstrumentDto>> GetPublicPagedAsync(InstrumentSearchObject search)
        {
            var query = AddIncludes(context.Set<Instrument>().AsNoTracking())
                .Where(x => x.IsActive);

            if (search.IsAvailable.HasValue && search.IsAvailable.Value)
            {
                query = query.Where(x => !x.InstrumentRentals.Any(r =>
                    r.RentalStatus == InstrumentRentalStatus.Approved ||
                    r.RentalStatus == InstrumentRentalStatus.Active));
            }

            query = AddFilter(search, query);

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, MapEntityToModel);
        }

        public async Task<InstrumentDto> CreateAsync(InstrumentCreateRequest request, int employeeAppUserId)
        {
            var storeId = await storeContext.GetActiveStoreAsync(employeeAppUserId);

            var entity = mapper.Map<Instrument>(request);

            entity.MusicStoreId = storeId;

            await BeforeCreateAsync(request, entity);

            context.Set<Instrument>().Add(entity);

            await context.SaveChangesAsync();

            entity = await AfterSaveAsync(entity);

            return MapEntityToModel(entity);
        }

        public async Task<InstrumentDto> UpdateAsync(int id, InstrumentUpdateRequest request, int employeeAppUserId)
        {
            var storeId = await storeContext.GetActiveStoreAsync(employeeAppUserId);

            var entity = await context.Set<Instrument>()
                .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == storeId && x.IsActive)
                ?? throw new NotFoundException(Messages.NotFound);

            mapper.Map(request, entity);

            await BeforeUpdateAsync(request, entity);

            await context.SaveChangesAsync();

            entity = await AfterSaveAsync(entity);

            return MapEntityToModel(entity);
        }

        public async Task DeleteAsync(int id, int employeeAppUserId)
        {
            var storeId = await storeContext.GetActiveStoreAsync(employeeAppUserId);

            var instrument = await context.Set<Instrument>()
                .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == storeId && x.IsActive)
                ?? throw new NotFoundException(Messages.NotFound);

            var hasBlockingRental = await context.Set<InstrumentRental>()
                .AnyAsync(r => r.InstrumentId == id &&
                               (r.RentalStatus == InstrumentRentalStatus.Approved ||
                                r.RentalStatus == InstrumentRentalStatus.Active));

            if (hasBlockingRental)
                throw new BusinessException(Messages.InstrumentDeleteBlocked);

            instrument.IsActive = false;

            await context.SaveChangesAsync();
        }

        private static IQueryable<Instrument> AddIncludes(IQueryable<Instrument> query)
        {
            return query.WithInstrumentDetails();
        }

        private static IQueryable<Instrument> AddFilter(InstrumentSearchObject search, IQueryable<Instrument> query)
        {
            if (!string.IsNullOrWhiteSpace(search.Model))
                query = query.Where(x => x.Model.Contains(search.Model));

            if (!string.IsNullOrWhiteSpace(search.Manufacturer))
                query = query.Where(x => x.Manufacturer.Contains(search.Manufacturer));

            if (search.InstrumentTypeId.HasValue)
                query = query.Where(x => x.InstrumentTypeId == search.InstrumentTypeId);

            if (search.IsAvailable.HasValue)
            {
                if (search.IsAvailable.Value)
                    query = query.Where(x => !x.InstrumentRentals.Any(r =>
                        r.RentalStatus == InstrumentRentalStatus.Approved ||
                        r.RentalStatus == InstrumentRentalStatus.Active));
                else
                    query = query.Where(x => x.InstrumentRentals.Any(r =>
                        r.RentalStatus == InstrumentRentalStatus.Approved ||
                        r.RentalStatus == InstrumentRentalStatus.Active));
            }

            return query;
        }

        private static Task BeforeUpdateAsync(InstrumentUpdateRequest request, Instrument entity)
        {
            return Task.CompletedTask;
        }

        private async Task BeforeCreateAsync(InstrumentCreateRequest request, Instrument entity)
        {
            var existingType = await context
                .Set<InstrumentType>()
                .AnyAsync(x => x.Id == request.InstrumentTypeId);

            if (!existingType)
                throw new BusinessException(Messages.InstrumentTypeNotFound);
        }

        private async Task<Instrument> AfterSaveAsync(Instrument entity)
        {
            return await context.Set<Instrument>()
                .AsNoTracking()
                .WithInstrumentDetails()
                .FirstAsync(x => x.Id == entity.Id);
        }
    }
}
