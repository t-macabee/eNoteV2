using eNote.Application.Common.Files;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using System.Linq.Expressions;

namespace eNote.Application.Features.Rentals.Instruments.Services;

public sealed class InstrumentService(
    IAppDbContext context,
    IStudentContext students,
    IFileStorageService fileStorage,
    RentalCommandService rentalCommands)
{
    private static readonly Expression<Func<Instrument, InstrumentDto>> ProjectToDto = x => new InstrumentDto
    {
        Id = x.Id,
        Model = x.Model,
        ManufacturerId = x.ManufacturerId,
        ManufacturerName = x.Manufacturer.Name,
        Description = x.Description,
        ImagePath = x.ImagePath,
        InstrumentTypeId = x.InstrumentTypeId,
        InstrumentType = x.InstrumentType.Type,
        MonthlyFee = x.InstrumentType.MonthlyFee,
        MusicStore = x.MusicStore.StoreName,
        IsAvailable = x.IsActive && !x.InstrumentRentals.Any(r =>
            InstrumentRentalStatusExtensions.BlockingStatuses.Contains(r.RentalStatus)),
        DeleteBlockedReason = x.InstrumentRentals.Any(r =>
            InstrumentRentalStatusExtensions.BlockingStatuses.Contains(r.RentalStatus))
            ? Messages.InstrumentDeleteBlocked
            : null
    };

    public async Task<InstrumentDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Instrument>()
            .AsNoTracking()
            .WhereActive();

        return await query
            .Where(x => x.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.InstrumentNotFound);
    }

    public async Task<PagedResult<InstrumentDto>> GetPagedAsync(InstrumentSearchObject search, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Instrument>()
            .AsNoTracking()
            .WhereActive();

        query = query.ApplySearch(search);

        var (page, pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);
        var total = search.IncludeTotalCount ? await query.CountAsync(cancellationToken) : (int?)null;

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ProjectToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<InstrumentDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<InstrumentDto> CreateAsync(InstrumentRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await EnsureStoreAccessAsync(cancellationToken);
        var type = await EnsureInstrumentTypeExistsAsync(request.InstrumentTypeId, cancellationToken);
        var manufacturer = await EnsureManufacturerExistsAsync(request.ManufacturerId, cancellationToken);
        var store = await LoadStoreAsync(employee.MusicStoreId, cancellationToken);

        var entity = new Instrument(
            request.Model.Trim(),
            request.ManufacturerId,
            NormalizeDescription(request.Description),
            null,
            request.InstrumentTypeId,
            employee.MusicStoreId);

        context.Set<Instrument>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return BuildDto(entity, type, manufacturer, store, hasBlockingRental: false);
    }

    public async Task<InstrumentDto> UpdateAsync(int id, InstrumentRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureStoreAccessAsync(cancellationToken);

        var entity = await context.Set<Instrument>()
            .WhereActive()
            .Include(x => x.MusicStore)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.InstrumentNotFound);

        var type = await EnsureInstrumentTypeExistsAsync(request.InstrumentTypeId, cancellationToken);
        var manufacturer = await EnsureManufacturerExistsAsync(request.ManufacturerId, cancellationToken);

        entity.UpdateDetails(
            request.Model.Trim(),
            request.ManufacturerId,
            NormalizeDescription(request.Description),
            entity.ImagePath,
            request.InstrumentTypeId);

        await context.SaveChangesAsync(cancellationToken);

        return BuildDto(entity, type, manufacturer, entity.MusicStore, await HasBlockingRentalAsync(entity.Id, cancellationToken));
    }

    public async Task<InstrumentDto> UploadImageAsync(int id, Stream stream, string contentType, CancellationToken ct = default)
    {
        await EnsureStoreAccessAsync(ct);

        var entity = await context.Set<Instrument>()
            .WhereActive()
            .Include(x => x.InstrumentType)
            .Include(x => x.Manufacturer)
            .Include(x => x.MusicStore)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(Messages.InstrumentNotFound);

        await StoreStorageQuota.EnsureWithinQuotaAsync(context, fileStorage, entity.MusicStoreId, stream.Length, entity.ImagePath, ct);

        var previousPath = entity.ImagePath;
        var path = await fileStorage.SaveAsync(stream, contentType, "instruments", ct);
        entity.UpdateDetails(entity.Model, entity.ManufacturerId, entity.Description, path, entity.InstrumentTypeId);

        await context.SaveChangesReplacingFileAsync(fileStorage, path, previousPath, ct);

        return BuildDto(entity, entity.InstrumentType, entity.Manufacturer, entity.MusicStore, await HasBlockingRentalAsync(entity.Id, ct));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await EnsureStoreAccessAsync(cancellationToken);

        var instrument = await context.Set<Instrument>()
            .WhereActive()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.InstrumentNotFound);

        await context.ExecuteInTransactionAsync(async () =>
        {
            var blockedReason = await context.Set<Instrument>()
                .AsNoTracking()
                .WhereActive()
                .Where(x => x.Id == id)
                .Select(ProjectToDto)
                .Select(x => x.DeleteBlockedReason)
                .FirstOrDefaultAsync(cancellationToken);

            if (blockedReason is not null)
            {
                throw new BusinessException(blockedReason);
            }

            await rentalCommands.RejectPendingForArchivedInstrumentAsync(id, employee.AppUserId, cancellationToken);

            instrument.SoftDelete();
            await context.SaveChangesMappingConflictAsync(Messages.Conflict, cancellationToken);
        }, cancellationToken);
    }

    private Task<MusicStoreEmployee> EnsureStoreAccessAsync(CancellationToken cancellationToken) =>
        students.GetCurrentEmployeeAsync(cancellationToken);

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private async Task<InstrumentType> EnsureInstrumentTypeExistsAsync(int instrumentTypeId, CancellationToken cancellationToken) =>
        await context.Set<InstrumentType>().FirstOrDefaultAsync(x => x.Id == instrumentTypeId, cancellationToken)
        ?? throw new BusinessException(Messages.InstrumentTypeNotFound);

    private async Task<Manufacturer> EnsureManufacturerExistsAsync(int manufacturerId, CancellationToken cancellationToken) =>
        await context.Set<Manufacturer>().FirstOrDefaultAsync(x => x.Id == manufacturerId, cancellationToken)
        ?? throw new BusinessException(Messages.ManufacturerNotFound);

    private async Task<MusicStore> LoadStoreAsync(int storeId, CancellationToken cancellationToken) =>
        await context.Set<MusicStore>().FirstAsync(x => x.Id == storeId, cancellationToken);

    private Task<bool> HasBlockingRentalAsync(int instrumentId, CancellationToken cancellationToken) =>
        context.Set<InstrumentRental>()
            .WhereBlockingStatus()
            .AnyAsync(r => r.InstrumentId == instrumentId, cancellationToken);

    private static InstrumentDto BuildDto(Instrument entity, InstrumentType type, Manufacturer manufacturer, MusicStore store, bool hasBlockingRental) => new()
    {
        Id = entity.Id,
        Model = entity.Model,
        ManufacturerId = entity.ManufacturerId,
        ManufacturerName = manufacturer.Name,
        Description = entity.Description,
        ImagePath = entity.ImagePath,
        InstrumentTypeId = entity.InstrumentTypeId,
        InstrumentType = type.Type,
        MonthlyFee = type.MonthlyFee,
        MusicStore = store.StoreName,
        IsAvailable = entity.IsActive && !hasBlockingRental,
        DeleteBlockedReason = hasBlockingRental ? Messages.InstrumentDeleteBlocked : null
    };
}