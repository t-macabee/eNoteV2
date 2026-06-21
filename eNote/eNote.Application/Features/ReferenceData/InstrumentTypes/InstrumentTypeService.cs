using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.ReferenceData.InstrumentTypes;

public sealed class InstrumentTypeService(IAppDbContext context) : IInstrumentTypeService
{
    public async Task<PagedResult<InstrumentTypeDto>> GetPagedAsync(int page, int pageSize)
    {
        IQueryable<InstrumentType> query = context.Set<InstrumentType>().AsNoTracking();

        return await query.ToPagedResultAsync(
            page,
            pageSize,
            includeTotalCount: true,
            MapToDto,
            q => q.OrderBy(x => x.Type));
    }

    public async Task<InstrumentTypeDto> GetByIdAsync(int id)
    {
        InstrumentType entity = await context.Set<InstrumentType>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(Messages.InstrumentTypeNotFound);

        return MapToDto(entity);
    }

    public async Task<InstrumentTypeDto> CreateAsync(InstrumentTypeRequest request)
    {
        var entity = new InstrumentType
        {
            Type = request.Type.Trim(),
            MonthlyFee = request.MonthlyFee
        };

        context.Set<InstrumentType>().Add(entity);
        await context.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<InstrumentTypeDto> UpdateAsync(int id, InstrumentTypeRequest request)
    {
        InstrumentType entity = await context.Set<InstrumentType>()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(Messages.InstrumentTypeNotFound);

        entity.Type = request.Type.Trim();
        entity.MonthlyFee = request.MonthlyFee;

        await context.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task DeleteAsync(int id)
    {
        InstrumentType entity = await context.Set<InstrumentType>()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(Messages.InstrumentTypeNotFound);

        bool inUse = await context.Set<Instrument>()
            .AnyAsync(x => x.InstrumentTypeId == id);

        if (inUse)
        {
            throw new BusinessException(Messages.InstrumentTypeDeleteBlocked);
        }

        context.Set<InstrumentType>().Remove(entity);
        await context.SaveChangesAsync();
    }

    private static InstrumentTypeDto MapToDto(InstrumentType entity) => new()
    {
        Id = entity.Id,
        Type = entity.Type,
        MonthlyFee = entity.MonthlyFee
    };
}
