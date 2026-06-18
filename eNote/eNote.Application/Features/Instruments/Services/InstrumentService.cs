using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Users.Services;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Instruments.Services
{
    public class InstrumentService(IAppDbContext context, IMapper mapper, IUserContextResolver resolver, ICurrentUserService currentUserService, IFileStorageService fileStorage) : IInstrumentService
    {
        public async Task<InstrumentDto> GetByIdAsync(int id)
        {
            MusicStoreEmployee employee = await EnsureStoreAccessAsync();

            IQueryable<Instrument> query = AddIncludes(context.Set<Instrument>()
                .AsNoTracking()
                .Where(x => x.MusicStoreId == employee.MusicStoreId));

            Instrument entity = await query
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new NotFoundException(Messages.NotFound);

            return mapper.Map<InstrumentDto>(entity);
        }

        public async Task<InstrumentDto> GetPublicByIdAsync(int id)
        {
            Instrument entity = await AddIncludes(context.Set<Instrument>().AsNoTracking())
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new NotFoundException(Messages.NotFound);

            return mapper.Map<InstrumentDto>(entity);
        }

        public async Task<PagedResult<InstrumentDto>> GetPagedAsync(InstrumentSearchObject search)
        {
            MusicStoreEmployee employee = await EnsureStoreAccessAsync();

            IQueryable<Instrument> query = context.Set<Instrument>()
                .AsNoTracking()
                .Where(x => x.MusicStoreId == employee.MusicStoreId);

            query = AddIncludes(query);
            query = query.ApplySearch(search);

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, entity => mapper.Map<InstrumentDto>(entity));
        }

        public async Task<PagedResult<InstrumentDto>> GetPublicPagedAsync(InstrumentSearchObject search)
        {
            IQueryable<Instrument> query = AddIncludes(context.Set<Instrument>().AsNoTracking());
            query = query.ApplySearch(search);

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, entity => mapper.Map<InstrumentDto>(entity));
        }

        public async Task<InstrumentDto> CreateAsync(InstrumentCreateRequest request)
        {
            MusicStoreEmployee employee = await EnsureStoreAccessAsync();

            var entity = new Instrument(request.Model.Trim(), request.Manufacturer.Trim(), request.Description?.Trim(), request.ImagePath?.Trim(), request.InstrumentTypeId, employee.MusicStoreId);

            await BeforeCreateAsync(request);
            context.Set<Instrument>().Add(entity);

            await context.SaveChangesAsync();
            entity = await AfterSaveAsync(entity);

            return mapper.Map<InstrumentDto>(entity);
        }

        public async Task<InstrumentDto> UpdateAsync(int id, InstrumentUpdateRequest request)
        {
            MusicStoreEmployee employee = await EnsureStoreAccessAsync();

            Instrument entity = await context.Set<Instrument>()
                .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == employee.MusicStoreId)
                ?? throw new NotFoundException(Messages.NotFound);

            if (request.InstrumentTypeId.HasValue)
            {
                bool typeExists = await context.Set<InstrumentType>()
                    .AnyAsync(x => x.Id == request.InstrumentTypeId.Value);

                if (!typeExists)
                {
                    throw new BusinessException(Messages.InstrumentTypeNotFound);
                }
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

            return mapper.Map<InstrumentDto>(entity);
        }

        public async Task<InstrumentDto> UploadImageAsync(int id, Stream stream, string fileName, string contentType, CancellationToken ct = default)
        {
            MusicStoreEmployee employee = await EnsureStoreAccessAsync();

            Instrument entity = await context.Set<Instrument>()
                .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == employee.MusicStoreId, ct)
                ?? throw new NotFoundException(Messages.InstrumentNotFound);

            string path = await fileStorage.SaveAsync(stream, fileName, contentType, "instruments", ct);

            entity.UpdateDetails(entity.Model, entity.Manufacturer, entity.Description, path, entity.InstrumentTypeId);

            await context.SaveChangesAsync(ct);

            entity = await AfterSaveAsync(entity);
            return mapper.Map<InstrumentDto>(entity);
        }

        public async Task DeleteAsync(int id)
        {
            MusicStoreEmployee employee = await EnsureStoreAccessAsync();

            Instrument instrument = await context.Set<Instrument>()
                .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == employee.MusicStoreId)
                ?? throw new NotFoundException(Messages.NotFound);

            bool hasBlockingRental = await context.Set<InstrumentRental>()
                .AnyAsync(r => r.InstrumentId == id &&
                               (r.RentalStatus == InstrumentRentalStatus.Approved ||
                                r.RentalStatus == InstrumentRentalStatus.Active));

            if (hasBlockingRental)
            {
                throw new BusinessException(Messages.InstrumentDeleteBlocked);
            }

            instrument.SoftDelete();

            await context.SaveChangesAsync();
        }

        private Task<MusicStoreEmployee> EnsureStoreAccessAsync() =>
            resolver.GetActiveEmployeeAsync(currentUserService.UserId);

        private static IQueryable<Instrument> AddIncludes(IQueryable<Instrument> query)
        {
            return query.WithInstrumentDetails();
        }

        private async Task BeforeCreateAsync(InstrumentCreateRequest request)
        {
            bool existingType = await context
                .Set<InstrumentType>()
                .AnyAsync(x => x.Id == request.InstrumentTypeId);

            if (!existingType)
            {
                throw new BusinessException(Messages.InstrumentTypeNotFound);
            }
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
