using MapsterMapper;

namespace eNote.Application.Features.Rentals.Instruments.Services;

public sealed class InstrumentService(
    IAppDbContext context,
    IMapper mapper,
    IStudentContext students,
    IFileStorageService fileStorage)
{
    public async Task<InstrumentDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Set<Instrument>()
            .AsNoTracking()
            .WithInstrumentDetails()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound);

        return mapper.Map<InstrumentDto>(entity);
    }

    public async Task<InstrumentDto> GetPublicByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Set<Instrument>()
            .AsNoTracking()
            .WithInstrumentDetails()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound);

        return mapper.Map<InstrumentDto>(entity);
    }

    public async Task<PagedResult<InstrumentDto>> GetPagedAsync(InstrumentSearchObject search, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Instrument>()
            .AsNoTracking()
            .WithInstrumentDetails()
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<InstrumentDto>, ct: cancellationToken);
    }

    public async Task<PagedResult<InstrumentDto>> GetPublicPagedAsync(InstrumentSearchObject search, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Instrument>()
            .AsNoTracking()
            .WithInstrumentDetails()
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<InstrumentDto>, ct: cancellationToken);
    }

    public async Task<InstrumentDto> CreateAsync(InstrumentCreateRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await EnsureStoreAccessAsync();
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

        return mapper.Map<InstrumentDto>(await ReloadAsync(entity.Id, cancellationToken));
    }

    public async Task<InstrumentDto> UpdateAsync(int id, InstrumentUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureStoreAccessAsync();

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

        return mapper.Map<InstrumentDto>(await ReloadAsync(entity.Id, cancellationToken));
    }

    public async Task<InstrumentDto> UploadImageAsync(int id, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        await EnsureStoreAccessAsync();

        var entity = await context.Set<Instrument>()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(Messages.InstrumentNotFound);

        var path = await fileStorage.SaveAsync(stream, fileName, contentType, "instruments", ct);
        entity.UpdateDetails(entity.Model, entity.Manufacturer, entity.Description, path, entity.InstrumentTypeId);

        await context.SaveChangesAsync(ct);

        return mapper.Map<InstrumentDto>(await ReloadAsync(entity.Id, ct));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await EnsureStoreAccessAsync();

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

    private Task<MusicStoreEmployee> EnsureStoreAccessAsync() =>
        students.GetCurrentEmployeeAsync();

    private async Task EnsureInstrumentTypeExistsAsync(int instrumentTypeId, CancellationToken cancellationToken)
    {
        if (!await context.Set<InstrumentType>().AnyAsync(x => x.Id == instrumentTypeId, cancellationToken))
        {
            throw new BusinessException(Messages.InstrumentTypeNotFound);
        }
    }

    private Task<Instrument> ReloadAsync(int id, CancellationToken cancellationToken) =>
        context.Set<Instrument>().AsNoTracking().WithInstrumentDetails().FirstAsync(x => x.Id == id, cancellationToken);
}