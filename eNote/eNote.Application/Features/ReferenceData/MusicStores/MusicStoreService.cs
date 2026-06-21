using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.ReferenceData.MusicStores;

public sealed class MusicStoreService(IAppDbContext context) : IMusicStoreService
{
    public async Task<PagedResult<MusicStoreDto>> GetPagedAsync(int page, int pageSize)
    {
        IQueryable<MusicStore> query = context.Set<MusicStore>().AsNoTracking();

        return await query.ToPagedResultAsync(
            page,
            pageSize,
            includeTotalCount: true,
            MapToDto,
            q => q.OrderBy(x => x.StoreName));
    }

    public async Task<MusicStoreDto> GetByIdAsync(int id)
    {
        MusicStore entity = await context.Set<MusicStore>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(Messages.StoreNotFound);

        return MapToDto(entity);
    }

    public async Task<MusicStoreDto> CreateAsync(MusicStoreRequest request)
    {
        var entity = new MusicStore(request.StoreName.Trim(), request.BusinessHours.Trim());

        context.Set<MusicStore>().Add(entity);
        await context.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<MusicStoreDto> UpdateAsync(int id, MusicStoreRequest request)
    {
        MusicStore entity = await context.Set<MusicStore>()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(Messages.StoreNotFound);

        entity.UpdateDetails(request.StoreName.Trim(), request.BusinessHours.Trim());

        await context.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task DeleteAsync(int id)
    {
        MusicStore entity = await context.Set<MusicStore>()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(Messages.StoreNotFound);

        bool hasInstruments = await context.Set<Instrument>().AnyAsync(x => x.MusicStoreId == id);
        bool hasEmployees = await context.Set<MusicStoreEmployee>().AnyAsync(x => x.MusicStoreId == id);

        if (hasInstruments || hasEmployees)
        {
            throw new BusinessException(Messages.MusicStoreDeleteBlocked);
        }

        context.Set<MusicStore>().Remove(entity);
        await context.SaveChangesAsync();
    }

    private static MusicStoreDto MapToDto(MusicStore entity) => new()
    {
        Id = entity.Id,
        StoreName = entity.StoreName,
        BusinessHours = entity.BusinessHours
    };
}
