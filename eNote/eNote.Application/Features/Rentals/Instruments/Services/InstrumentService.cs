using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Rentals.Instruments;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Rentals.Instruments.Services;

public sealed class InstrumentService(
    IAppDbContext context,
    IMapper mapper,
    ICurrentActor actor,
    IFileStorageService fileStorage) : IInstrumentService
{
    public async Task<InstrumentDto> GetByIdAsync(int id)
    {
        var employee = await EnsureStoreAccessAsync();

        var entity = await context.Set<Instrument>()
            .AsNoTracking()
            .WithInstrumentDetails()
            .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == employee.MusicStoreId)
            ?? throw new NotFoundException(Messages.NotFound);

        return mapper.Map<InstrumentDto>(entity);
    }

    public async Task<InstrumentDto> GetPublicByIdAsync(int id)
    {
        var entity = await context.Set<Instrument>()
            .AsNoTracking()
            .WithInstrumentDetails()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(Messages.NotFound);

        return mapper.Map<InstrumentDto>(entity);
    }

    public async Task<PagedResult<InstrumentDto>> GetPagedAsync(InstrumentSearchObject search)
    {
        var employee = await EnsureStoreAccessAsync();

        var query = context.Set<Instrument>()
            .AsNoTracking()
            .Where(x => x.MusicStoreId == employee.MusicStoreId)
            .WithInstrumentDetails()
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<InstrumentDto>);
    }

    public async Task<PagedResult<InstrumentDto>> GetPublicPagedAsync(InstrumentSearchObject search)
    {
        var query = context.Set<Instrument>()
            .AsNoTracking()
            .WithInstrumentDetails()
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<InstrumentDto>);
    }

    public async Task<InstrumentDto> CreateAsync(InstrumentCreateRequest request)
    {
        var employee = await EnsureStoreAccessAsync();
        await EnsureInstrumentTypeExistsAsync(request.InstrumentTypeId);

        var entity = new Instrument(
            request.Model.Trim(),
            request.Manufacturer.Trim(),
            request.Description?.Trim(),
            request.ImagePath?.Trim(),
            request.InstrumentTypeId,
            employee.MusicStoreId);

        context.Set<Instrument>().Add(entity);
        await context.SaveChangesAsync();

        return mapper.Map<InstrumentDto>(await ReloadAsync(entity.Id));
    }

    public async Task<InstrumentDto> UpdateAsync(int id, InstrumentUpdateRequest request)
    {
        var employee = await EnsureStoreAccessAsync();

        var entity = await context.Set<Instrument>()
            .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == employee.MusicStoreId)
            ?? throw new NotFoundException(Messages.NotFound);

        if (request.InstrumentTypeId is int typeId)
        {
            await EnsureInstrumentTypeExistsAsync(typeId);
        }

        entity.UpdateDetails(
            request.Model?.Trim() ?? entity.Model,
            request.Manufacturer?.Trim() ?? entity.Manufacturer,
            request.Description?.Trim() ?? entity.Description,
            request.ImagePath?.Trim() ?? entity.ImagePath,
            request.InstrumentTypeId ?? entity.InstrumentTypeId);

        await context.SaveChangesAsync();

        return mapper.Map<InstrumentDto>(await ReloadAsync(entity.Id));
    }

    public async Task<InstrumentDto> UploadImageAsync(int id, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var employee = await EnsureStoreAccessAsync();

        var entity = await context.Set<Instrument>()
            .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == employee.MusicStoreId, ct)
            ?? throw new NotFoundException(Messages.InstrumentNotFound);

        var path = await fileStorage.SaveAsync(stream, fileName, contentType, "instruments", ct);
        entity.UpdateDetails(entity.Model, entity.Manufacturer, entity.Description, path, entity.InstrumentTypeId);

        await context.SaveChangesAsync(ct);

        return mapper.Map<InstrumentDto>(await ReloadAsync(entity.Id));
    }

    public async Task DeleteAsync(int id)
    {
        var employee = await EnsureStoreAccessAsync();

        var instrument = await context.Set<Instrument>()
            .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == employee.MusicStoreId)
            ?? throw new NotFoundException(Messages.NotFound);

        if (await context.Set<InstrumentRental>().WhereBlockingStatus().AnyAsync(r => r.InstrumentId == id))
        {
            throw new BusinessException(Messages.InstrumentDeleteBlocked);
        }

        instrument.SoftDelete();
        await context.SaveChangesAsync();
    }

    private Task<MusicStoreEmployee> EnsureStoreAccessAsync() =>
        actor.GetCurrentEmployeeAsync();

    private async Task EnsureInstrumentTypeExistsAsync(int instrumentTypeId)
    {
        if (!await context.Set<InstrumentType>().AnyAsync(x => x.Id == instrumentTypeId))
        {
            throw new BusinessException(Messages.InstrumentTypeNotFound);
        }
    }

    private Task<Instrument> ReloadAsync(int id) =>
        context.Set<Instrument>().AsNoTracking().WithInstrumentDetails().FirstAsync(x => x.Id == id);
}
