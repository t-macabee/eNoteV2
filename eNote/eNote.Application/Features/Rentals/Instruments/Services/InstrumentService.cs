using System.Linq.Expressions;

namespace eNote.Application.Features.Rentals.Instruments.Services;

public sealed class InstrumentService(
    IAppDbContext context,
    IStudentContext students,
    IFileStorageService fileStorage)
{
    private static readonly Expression<Func<Instrument, InstrumentDto>> ProjectToDto = x => new InstrumentDto
    {
        Id = x.Id,
        Model = x.Model,
        Manufacturer = x.Manufacturer,
        Description = x.Description,
        ImagePath = x.ImagePath,
        InstrumentTypeId = x.InstrumentTypeId,
        InstrumentType = x.InstrumentType.Type,
        MusicStore = x.MusicStore.StoreName,
        IsAvailable = x.IsActive && !x.InstrumentRentals.Any(r =>
            r.RentalStatus == InstrumentRentalStatus.Approved ||
            r.RentalStatus == InstrumentRentalStatus.Active)
    };

    public async Task<InstrumentDto> GetByIdAsync(int id, bool publicView = false, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Instrument>()
            .AsNoTracking();

        if (publicView)
        {
            query = query.IgnoreQueryFilters().Where(x => x.IsActive);
        }

        return await query
            .Where(x => x.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound);
    }

    public async Task<PagedResult<InstrumentDto>> GetPagedAsync(InstrumentSearchObject search, bool publicView = false, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Instrument>()
            .AsNoTracking();

        if (publicView)
        {
            query = query.IgnoreQueryFilters().Where(x => x.IsActive);
        }

        query = query.ApplySearch(search);

        var (page, pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);
        var total = search.IncludeTotalCount ? await query.CountAsync(cancellationToken) : (int?)null;

        var items = await query
            .OrderBy(x => x.Id)
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

    public async Task<InstrumentDto> CreateAsync(InstrumentCreateRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await EnsureStoreAccessAsync(cancellationToken);
        await EnsureInstrumentTypeExistsAsync(request.InstrumentTypeId, cancellationToken);

        var entity = new Instrument(
            request.Model.Trim(),
            request.Manufacturer.Trim(),
            request.Description?.Trim(),
            request.ImagePath?.Trim(),
            request.InstrumentTypeId,
            employee.MusicStoreId);

        context.Set<Instrument>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return await ReloadAsync(entity.Id, cancellationToken);
    }

    public async Task<InstrumentDto> UpdateAsync(int id, InstrumentUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureStoreAccessAsync(cancellationToken);

        var entity = await context.Set<Instrument>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound);

        if (request.InstrumentTypeId is { } typeId)
        {
            await EnsureInstrumentTypeExistsAsync(typeId, cancellationToken);
        }

        entity.UpdateDetails(
            request.Model?.Trim() ?? entity.Model,
            request.Manufacturer?.Trim() ?? entity.Manufacturer,
            request.Description?.Trim() ?? entity.Description,
            request.ImagePath?.Trim() ?? entity.ImagePath,
            request.InstrumentTypeId ?? entity.InstrumentTypeId);

        await context.SaveChangesAsync(cancellationToken);

        return await ReloadAsync(entity.Id, cancellationToken);
    }

    public async Task<InstrumentDto> UploadImageAsync(int id, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        await EnsureStoreAccessAsync(ct);

        var entity = await context.Set<Instrument>()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(Messages.InstrumentNotFound);

        var previousPath = entity.ImagePath;
        var path = await fileStorage.SaveAsync(stream, fileName, contentType, "instruments", ct);
        entity.UpdateDetails(entity.Model, entity.Manufacturer, entity.Description, path, entity.InstrumentTypeId);

        await context.SaveChangesReplacingFileAsync(fileStorage, path, previousPath, ct);

        return await ReloadAsync(entity.Id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await EnsureStoreAccessAsync(cancellationToken);

        var instrument = await context.Set<Instrument>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound);

        if (await context.Set<InstrumentRental>().WhereBlockingStatus().AnyAsync(r => r.InstrumentId == id, cancellationToken))
        {
            throw new BusinessException(Messages.InstrumentDeleteBlocked);
        }

        instrument.SoftDelete();
        await context.SaveChangesAsync(cancellationToken);
    }

    private Task<MusicStoreEmployee> EnsureStoreAccessAsync(CancellationToken cancellationToken) =>
        students.GetCurrentEmployeeAsync(cancellationToken);

    private async Task EnsureInstrumentTypeExistsAsync(int instrumentTypeId, CancellationToken cancellationToken)
    {
        if (!await context.Set<InstrumentType>().AnyAsync(x => x.Id == instrumentTypeId, cancellationToken))
        {
            throw new BusinessException(Messages.InstrumentTypeNotFound);
        }
    }

    private Task<InstrumentDto> ReloadAsync(int id, CancellationToken cancellationToken) =>
        context.Set<Instrument>().AsNoTracking().Where(x => x.Id == id).Select(ProjectToDto).FirstAsync(cancellationToken);
}