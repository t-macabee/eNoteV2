using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Rentals.ReferenceData;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public sealed class MusicStoreService(IAppDbContext context) : ReferenceCrudService<MusicStore, MusicStoreDto, MusicStoreRequest, MusicStoreSearchObject>(context), IMusicStoreService
{
    protected override string NotFoundMessage => Messages.StoreNotFound;

    protected override MusicStoreDto Map(MusicStore entity) => new()
    {
        Id = entity.Id,
        StoreName = entity.StoreName,
        BusinessHours = entity.BusinessHours
    };

    protected override MusicStore CreateEntity(MusicStoreRequest request) => new(request.StoreName.Trim(), request.BusinessHours.Trim());

    protected override void ApplyUpdate(MusicStore entity, MusicStoreRequest request) => entity.UpdateDetails(request.StoreName.Trim(), request.BusinessHours.Trim());
    protected override IQueryable<MusicStore> ApplySearch(IQueryable<MusicStore> query, MusicStoreSearchObject search) => query.ApplySearch(search);
    protected override IOrderedQueryable<MusicStore> Order(IQueryable<MusicStore> query) => query.OrderBy(x => x.StoreName);

    protected override async Task EnsureDeletableAsync(MusicStore entity, CancellationToken ct = default)
    {
        var inUse = await Db.Set<Instrument>().AnyAsync(x => x.MusicStoreId == entity.Id, ct)
            || await Db.Set<MusicStoreEmployee>().AnyAsync(x => x.MusicStoreId == entity.Id, ct);

        if (inUse)
        {
            throw new BusinessException(Messages.MusicStoreDeleteBlocked);
        }
    }
}