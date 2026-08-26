using eNote.Application.Common.Crud;
using eNote.Application.Common.Persistence;
using eNote.Domain.Entities.Rentals;

namespace eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;

public sealed class InstrumentTypeService(IAppDbContext context) : ReferenceDataCrudService<InstrumentType, InstrumentTypeDto, InstrumentTypeRequest, InstrumentTypeSearchObject>(context)
{
    protected override InstrumentTypeDto Map(InstrumentType entity) => new()
    {
        Id = entity.Id,
        Type = entity.Type,
        MonthlyFee = entity.MonthlyFee
    };

    protected override InstrumentType CreateEntity(InstrumentTypeRequest request) => new()
    {
        Type = request.Type.Trim(),
        MonthlyFee = request.MonthlyFee
    };

    protected override void UpdateEntity(InstrumentType entity, InstrumentTypeRequest request)
    {
        entity.Type = request.Type.Trim();
        entity.MonthlyFee = request.MonthlyFee;
    }

    protected override IQueryable<InstrumentType> ApplySearch(IQueryable<InstrumentType> query, InstrumentTypeSearchObject search)
    {
        return query.ApplySearch(search);
    }

    protected override IOrderedQueryable<InstrumentType> ApplyDefaultOrder(IQueryable<InstrumentType> query)
    {
        return query.OrderBy(x => x.Type);
    }

    protected override string NotFoundMessage => Messages.InstrumentTypeNotFound;

    protected override async Task EnsureDeletableAsync(InstrumentType entity, CancellationToken ct)
    {
        if (await Db.Set<Instrument>().AnyAsync(x => x.InstrumentTypeId == entity.Id, ct))
        {
            throw new BusinessException(Messages.InstrumentTypeDeleteBlocked);
        }
    }
}
