namespace eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;

public sealed class InstrumentTypeService(IAppDbContext context)
{
    private IAppDbContext Db => context;

    public Task<PagedResult<InstrumentTypeDto>> GetPagedAsync(InstrumentTypeSearchObject search, CancellationToken cancellationToken = default) =>
        Db.Set<InstrumentType>().AsNoTracking()
            .ApplySearch(search)
            .ToPagedResultAsync(search, Map, q => q.OrderBy(x => x.Type), ct: cancellationToken);

    public async Task<InstrumentTypeDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<InstrumentType>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.InstrumentTypeNotFound);

        return Map(entity);
    }

    public async Task<InstrumentTypeDto> CreateAsync(InstrumentTypeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new InstrumentType
        {
            Type = request.Type.Trim(),
            MonthlyFee = request.MonthlyFee
        };

        Db.Set<InstrumentType>().Add(entity);
        await Db.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<InstrumentTypeDto> UpdateAsync(int id, InstrumentTypeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<InstrumentType>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.InstrumentTypeNotFound);

        entity.Type = request.Type.Trim();
        entity.MonthlyFee = request.MonthlyFee;

        await Db.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<InstrumentType>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.InstrumentTypeNotFound);

        if (await Db.Set<Instrument>().AnyAsync(x => x.InstrumentTypeId == entity.Id, cancellationToken))
        {
            throw new BusinessException(Messages.InstrumentTypeDeleteBlocked);
        }

        Db.Set<InstrumentType>().Remove(entity);
        await Db.SaveChangesAsync(cancellationToken);
    }

    private static InstrumentTypeDto Map(InstrumentType entity) => new()
    {
        Id = entity.Id,
        Type = entity.Type,
        MonthlyFee = entity.MonthlyFee
    };
}
