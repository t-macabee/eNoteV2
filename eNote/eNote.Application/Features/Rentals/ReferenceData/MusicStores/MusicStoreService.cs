using eNote.Application.Common.Crud;

namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public sealed class MusicStoreService(IAppDbContext context) : ReferenceDataCrudService<MusicStore, MusicStoreDto, MusicStoreRequest, MusicStoreSearchObject>(context)
{
    protected override MusicStoreDto Map(MusicStore entity) => new()
    {
        Id = entity.Id,
        StoreName = entity.StoreName,
        BusinessHours = entity.BusinessHours
    };

    protected override MusicStore CreateEntity(MusicStoreRequest request) => new(request.StoreName.Trim(), request.BusinessHours.Trim());

    protected override void UpdateEntity(MusicStore entity, MusicStoreRequest request)
    {
        entity.UpdateDetails(request.StoreName.Trim(), request.BusinessHours.Trim());
    }

    protected override IQueryable<MusicStore> ApplySearch(IQueryable<MusicStore> query, MusicStoreSearchObject search)
    {
        return query.ApplySearch(search);
    }

    protected override IOrderedQueryable<MusicStore> ApplyDefaultOrder(IQueryable<MusicStore> query)
    {
        return query.OrderBy(x => x.StoreName);
    }

    protected override string NotFoundMessage => Messages.StoreNotFound;

    protected override async Task EnsureDeletableAsync(MusicStore entity, CancellationToken ct)
    {
        var inUse = await Db.Set<Instrument>().AnyAsync(x => x.MusicStoreId == entity.Id, ct)
            || await Db.Set<MusicStoreEmployee>().AnyAsync(x => x.MusicStoreId == entity.Id, ct);

        if (inUse)
        {
            throw new BusinessException(Messages.MusicStoreDeleteBlocked);
        }
    }
}
