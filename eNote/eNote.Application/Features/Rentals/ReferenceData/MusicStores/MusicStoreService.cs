using eNote.Application.Common.Crud;

namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public sealed class MusicStoreService(IAppDbContext context, IFileStorageService fileStorage) : ReferenceDataCrudService<MusicStore, MusicStoreDto, MusicStoreRequest, MusicStoreSearchObject>(context)
{
    protected override MusicStoreDto Map(MusicStore entity) => new()
    {
        Id = entity.Id,
        StoreName = entity.StoreName,
        BusinessHours = entity.BusinessHours,
        PhoneNumber = entity.PhoneNumber,
        ImagePath = entity.ImagePath,
        AddressId = entity.AddressId,
        AddressStreet = entity.Address?.Street,
        AddressCity = entity.Address?.City?.Name
    };

    protected override MusicStore CreateEntity(MusicStoreRequest request) => new(
        request.StoreName.Trim(),
        request.BusinessHours.Trim(),
        request.AddressId,
        request.PhoneNumber?.Trim());

    protected override void UpdateEntity(MusicStore entity, MusicStoreRequest request)
    {
        entity.UpdateDetails(
            request.StoreName.Trim(),
            request.BusinessHours.Trim(),
            request.AddressId,
            request.PhoneNumber?.Trim());
    }

    public override async Task<PagedResult<MusicStoreDto>> GetPagedAsync(MusicStoreSearchObject search, CancellationToken cancellationToken = default)
    {
        IQueryable<MusicStore> query = Db.Set<MusicStore>().AsNoTracking().Include(x => x.Address).ThenInclude(a => a!.City);
        query = ApplySearch(query, search);
        query = ApplyDefaultOrder(query);
        return await query.ToPagedResultAsync(search, Map, ct: cancellationToken);
    }

    public override async Task<MusicStoreDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<MusicStore>().AsNoTracking().Include(x => x.Address).ThenInclude(a => a!.City).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);
        return Map(entity);
    }

    public override async Task<MusicStoreDto> CreateAsync(MusicStoreRequest request, CancellationToken cancellationToken = default)
    {
        var entity = CreateEntity(request);
        Db.Set<MusicStore>().Add(entity);
        await Db.SaveChangesAsync(cancellationToken);
        var reloaded = await Db.Set<MusicStore>().AsNoTracking().Include(x => x.Address).ThenInclude(a => a!.City).FirstOrDefaultAsync(x => x.Id == entity.Id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);
        return Map(reloaded);
    }

    public override async Task<MusicStoreDto> UpdateAsync(int id, MusicStoreRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<MusicStore>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);
        UpdateEntity(entity, request);
        await Db.SaveChangesAsync(cancellationToken);
        var reloaded = await Db.Set<MusicStore>().AsNoTracking().Include(x => x.Address).ThenInclude(a => a!.City).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);
        return Map(reloaded);
    }

    public async Task<MusicStoreDto> UploadImageAsync(int id, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var entity = await Db.Set<MusicStore>().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(NotFoundMessage);

        var path = await fileStorage.SaveAsync(stream, fileName, contentType, "music-stores", ct);
        entity.UpdateImagePath(path);

        await Db.SaveChangesAsync(ct);

        var reloaded = await Db.Set<MusicStore>().AsNoTracking().Include(x => x.Address).ThenInclude(a => a!.City).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(NotFoundMessage);

        return Map(reloaded);
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
