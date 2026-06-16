using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Queryable;
using eNote.Application.Features.Instruments.Search;
using eNote.Application.Features.Instruments.Services.Interfaces;
using eNote.Application.Features.Users;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Instruments.Services
{
    public class InstrumentService(IAppDbContext context, IMapper mapper, ICurrentUserService currentUserService) : IInstrumentService
    {
        private InstrumentDto MapEntityToModel(Instrument entity) => mapper.Map<InstrumentDto>(entity);

        public async Task<InstrumentDto> GetByIdAsync(int id)
        {
            var employee = await EnsureStoreAccessAsync();

            var query = AddIncludes(context.Set<Instrument>()
                .AsNoTracking()
                .Where(x => x.MusicStoreId == employee.MusicStoreId));

            var entity = await query
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new NotFoundException(Messages.NotFound);

            return MapEntityToModel(entity);
        }

        public async Task<InstrumentDto> GetPublicByIdAsync(int id)
        {
            var entity = await AddIncludes(context.Set<Instrument>().AsNoTracking())
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new NotFoundException(Messages.NotFound);

            return MapEntityToModel(entity);
        }

        public async Task<PagedResult<InstrumentDto>> GetPagedAsync(InstrumentSearchObject search)
        {
            var employee = await EnsureStoreAccessAsync();

            var query = context.Set<Instrument>()
                .AsNoTracking()
                .Where(x => x.MusicStoreId == employee.MusicStoreId);

            query = AddIncludes(query);
            query = AddFilter(search, query);

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, MapEntityToModel);
        }

        public async Task<PagedResult<InstrumentDto>> GetPublicPagedAsync(InstrumentSearchObject search)
        {
            var query = AddIncludes(context.Set<Instrument>().AsNoTracking());

            if (search.IsAvailable.HasValue && search.IsAvailable.Value)
            {
                query = query.Where(x => !x.InstrumentRentals.Any(r =>
                    r.RentalStatus == InstrumentRentalStatus.Approved ||
                    r.RentalStatus == InstrumentRentalStatus.Active));
            }

            query = AddFilter(search, query);

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, MapEntityToModel);
        }

        public async Task<InstrumentDto> CreateAsync(InstrumentCreateRequest request)
        {
            var employee = await EnsureStoreAccessAsync();

            var entity = new Instrument(
                request.Model.Trim(),
                request.Manufacturer.Trim(),
                request.Description?.Trim(),
                request.ImagePath?.Trim(),
                request.InstrumentTypeId,
                employee.MusicStoreId
            );

            await BeforeCreateAsync(request);
            context.Set<Instrument>().Add(entity);

            await context.SaveChangesAsync();
            entity = await AfterSaveAsync(entity);

            return MapEntityToModel(entity);
        }

        public async Task<InstrumentDto> UpdateAsync(int id, InstrumentUpdateRequest request)
        {
            var employee = await EnsureStoreAccessAsync();

            var entity = await context.Set<Instrument>()
                .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == employee.MusicStoreId)
                ?? throw new NotFoundException(Messages.NotFound);

            if (request.InstrumentTypeId.HasValue)
            {
                var typeExists = await context.Set<InstrumentType>()
                    .AnyAsync(x => x.Id == request.InstrumentTypeId.Value);

                if (!typeExists)
                    throw new BusinessException(Messages.InstrumentTypeNotFound);
            }

            entity.UpdateDetails(
                request.Model?.Trim() ?? entity.Model,
                request.Manufacturer?.Trim() ?? entity.Manufacturer,
                request.Description?.Trim() ?? entity.Description,
                request.ImagePath?.Trim() ?? entity.ImagePath,
                request.InstrumentTypeId ?? entity.InstrumentTypeId
            );

            await context.SaveChangesAsync();
            entity = await AfterSaveAsync(entity);

            return MapEntityToModel(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var employee = await EnsureStoreAccessAsync();

            var instrument = await context.Set<Instrument>()
                .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == employee.MusicStoreId)
                ?? throw new NotFoundException(Messages.NotFound);

            var hasBlockingRental = await context.Set<InstrumentRental>()
                .AnyAsync(r => r.InstrumentId == id &&
                               (r.RentalStatus == InstrumentRentalStatus.Approved ||
                                r.RentalStatus == InstrumentRentalStatus.Active));

            if (hasBlockingRental)
                throw new BusinessException(Messages.InstrumentDeleteBlocked);

            instrument.SoftDelete();

            await context.SaveChangesAsync();
        }

        private async Task<MusicStoreEmployee> EnsureStoreAccessAsync() =>
            await UserProfileHelper.GetActiveEmployeeByUserIdAsync(context, currentUserService.UserId);

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

        private async Task BeforeCreateAsync(InstrumentCreateRequest request)
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