using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.ReferenceData.InstrumentTypes;

public sealed class InstrumentTypeService(IAppDbContext context)
    : ReferenceCrudService<InstrumentType, InstrumentTypeDto, InstrumentTypeRequest>(context), IInstrumentTypeService
{
    protected override string NotFoundMessage => Messages.InstrumentTypeNotFound;

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

    protected override void ApplyUpdate(InstrumentType entity, InstrumentTypeRequest request)
    {
        entity.Type = request.Type.Trim();
        entity.MonthlyFee = request.MonthlyFee;
    }

    protected override IOrderedQueryable<InstrumentType> Order(IQueryable<InstrumentType> query) =>
        query.OrderBy(x => x.Type);

    protected override async Task EnsureDeletableAsync(InstrumentType entity, CancellationToken ct = default)
    {
        if (await Db.Set<Instrument>().AnyAsync(x => x.InstrumentTypeId == entity.Id, ct))
        {
            throw new BusinessException(Messages.InstrumentTypeDeleteBlocked);
        }
    }
}