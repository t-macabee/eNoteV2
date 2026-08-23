using eNote.Application.Common.Paging;
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public sealed class MusicStoreService(IAppDbContext context) : IMusicStoreService
{
    private IAppDbContext Db => context;

    public Task<PagedResult<MusicStoreDto>> GetPagedAsync(MusicStoreSearchObject search, CancellationToken cancellationToken = default) =>
        Db.Set<MusicStore>().AsNoTracking()
            .ApplySearch(search)
            .ToPagedResultAsync(search, Map, q => q.OrderBy(x => x.StoreName), ct: cancellationToken);

    public async Task<MusicStoreDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<MusicStore>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.StoreNotFound);

        return Map(entity);
    }

    public async Task<MusicStoreDto> CreateAsync(MusicStoreRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new MusicStore(request.StoreName.Trim(), request.BusinessHours.Trim());

        Db.Set<MusicStore>().Add(entity);
        await Db.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<MusicStoreDto> UpdateAsync(int id, MusicStoreRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<MusicStore>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.StoreNotFound);

        entity.UpdateDetails(request.StoreName.Trim(), request.BusinessHours.Trim());
        await Db.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<MusicStore>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.StoreNotFound);

        var inUse = await Db.Set<Instrument>().AnyAsync(x => x.MusicStoreId == entity.Id, cancellationToken)
            || await Db.Set<MusicStoreEmployee>().AnyAsync(x => x.MusicStoreId == entity.Id, cancellationToken);

        if (inUse)
        {
            throw new BusinessException(Messages.MusicStoreDeleteBlocked);
        }

        Db.Set<MusicStore>().Remove(entity);
        await Db.SaveChangesAsync(cancellationToken);
    }

    private static MusicStoreDto Map(MusicStore entity) => new()
    {
        Id = entity.Id,
        StoreName = entity.StoreName,
        BusinessHours = entity.BusinessHours
    };
}
