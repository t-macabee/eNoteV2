using eNote.Application.Common.Crud;
using eNote.Application.Common.Files;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.ReferenceData.Addresses;
using System.Linq.Expressions;

namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public sealed class MusicStoreService(
    IAppDbContext context,
    IFileStorageService fileStorage,
    ICurrentUserContext currentUser) : ReferenceDataCrudService<MusicStore, MusicStoreDto, MusicStoreRequest, MusicStoreSearchObject>(context)
{
    protected override Expression<Func<MusicStore, MusicStoreDto>> Projection => x => new MusicStoreDto
    {
        Id = x.Id,
        StoreName = x.StoreName,
        BusinessHours = x.BusinessHours,
        PhoneNumber = x.PhoneNumber,
        ImagePath = x.ImagePath ?? DefaultImages.MusicStore,
        AddressId = x.AddressId,
        AddressStreet = x.Address != null ? x.Address.Street : null,
        AddressCity = x.Address != null ? x.Address.City.Name : null,
        DeleteBlockedReason = Db.Set<Instrument>().IgnoreQueryFilters().Any(i => i.MusicStoreId == x.Id)
            || Db.Set<MusicStoreEmployee>().IgnoreQueryFilters().Any(e => e.MusicStoreId == x.Id)
            ? Messages.MusicStoreDeleteBlocked
            : null
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

    public async Task<MusicStoreDto> GetOwnStoreAsync(CancellationToken cancellationToken = default)
    {
        var employee = await UserProfileLookup.GetEmployeeAsync(Db, currentUser.UserId, cancellationToken);

        return await GetByIdAsync(employee.MusicStoreId, cancellationToken);
    }

    public async Task<MusicStoreDto> UpdateOwnStoreAsync(ShopStoreUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await UserProfileLookup.EnsureManagerAsync(Db, currentUser.UserId, cancellationToken);

        return await UpdateOperationalDetailsAsync(employee.MusicStoreId, request.BusinessHours, request.PhoneNumber, cancellationToken);
    }

    public async Task<MusicStoreDto> UploadOwnStoreImageAsync(Stream stream, string contentType, CancellationToken cancellationToken = default)
    {
        var employee = await UserProfileLookup.EnsureManagerAsync(Db, currentUser.UserId, cancellationToken);

        return await UploadImageAsync(employee.MusicStoreId, stream, contentType, cancellationToken);
    }

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

        return await GetByIdAsync(id, cancellationToken);
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

        return await GetByIdAsync(id, ct);
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
        var reason = await Db.Set<MusicStore>()
            .AsNoTracking()
            .Where(x => x.Id == entity.Id)
            .Select(Projection)
            .Select(x => x.DeleteBlockedReason)
            .FirstOrDefaultAsync(ct);

        if (reason is not null)
        {
            throw new BusinessException(reason);
        }
    }
}
