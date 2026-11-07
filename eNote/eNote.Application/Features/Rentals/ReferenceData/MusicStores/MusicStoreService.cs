using eNote.Application.Common.Crud;
using eNote.Application.Common.Files;
using eNote.Application.Features.ReferenceData.Addresses;

namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public sealed class MusicStoreService(IAppDbContext context, IFileStorageService fileStorage) : ReferenceDataCrudService<MusicStore, MusicStoreDto, MusicStoreRequest, MusicStoreSearchObject>(context)
{
    protected override MusicStoreDto Map(MusicStore entity) => new()
    {
        Id = entity.Id,
        StoreName = entity.StoreName,
        BusinessHours = entity.BusinessHours,
        PhoneNumber = entity.PhoneNumber,
        ImagePath = entity.ImagePath ?? DefaultImages.MusicStore,
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
        var query = ApplySearch(WithAddress(), search);
        return await query.ToPagedResultAsync(search, Map, ApplyDefaultOrder, ct: cancellationToken);
    }

    public override async Task<MusicStoreDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await LoadDtoAsync(id, cancellationToken);

    public override async Task<MusicStoreDto> CreateAsync(MusicStoreRequest request, CancellationToken cancellationToken = default)
    {
        await Db.EnsureAddressExistsAsync(request.AddressId, cancellationToken);
        return await base.CreateAsync(request, cancellationToken);
    }

    public override async Task<MusicStoreDto> UpdateAsync(int id, MusicStoreRequest request, CancellationToken cancellationToken = default)
    {
        await Db.EnsureAddressExistsAsync(request.AddressId, cancellationToken);
        return await base.UpdateAsync(id, request, cancellationToken);
    }

    public async Task<MusicStoreDto> UpdateOperationalDetailsAsync(int id, string businessHours, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<MusicStore>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);

        entity.UpdateDetails(
            entity.StoreName,
            businessHours.Trim(),
            entity.AddressId,
            phoneNumber?.Trim());

        await Db.SaveChangesAsync(cancellationToken);

        return await LoadDtoAsync(id, cancellationToken);
    }

    public override async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var imagePath = await Db.Set<MusicStore>()
            .Where(x => x.Id == id)
            .Select(x => x.ImagePath)
            .FirstOrDefaultAsync(cancellationToken);

        await base.DeleteAsync(id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            fileStorage.Delete(imagePath);
        }
    }

    public async Task<MusicStoreDto> UploadImageAsync(int id, Stream stream, string contentType, CancellationToken ct = default)
    {
        var entity = await Db.Set<MusicStore>().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(NotFoundMessage);

        await StoreStorageQuota.EnsureWithinQuotaAsync(Db, fileStorage, id, stream.Length, entity.ImagePath, ct);

        var previousPath = entity.ImagePath;
        var path = await fileStorage.SaveAsync(stream, contentType, "music-stores", ct);
        entity.UpdateImagePath(path);

        await Db.SaveChangesReplacingFileAsync(fileStorage, path, previousPath, ct);

        return await LoadDtoAsync(id, ct);
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

    private IQueryable<MusicStore> WithAddress() =>
        Db.Set<MusicStore>().AsNoTracking().Include(x => x.Address).ThenInclude(a => a!.City);

    private async Task<MusicStoreDto> LoadDtoAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await WithAddress().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);

        return Map(entity);
    }

    protected override async Task EnsureDeletableAsync(MusicStore entity, CancellationToken ct)
    {
        var inUse = await Db.Set<Instrument>().IgnoreQueryFilters().AnyAsync(x => x.MusicStoreId == entity.Id, ct)
            || await Db.Set<MusicStoreEmployee>().AnyAsync(x => x.MusicStoreId == entity.Id, ct);

        if (inUse)
        {
            throw new BusinessException(Messages.MusicStoreDeleteBlocked);
        }
    }
}
