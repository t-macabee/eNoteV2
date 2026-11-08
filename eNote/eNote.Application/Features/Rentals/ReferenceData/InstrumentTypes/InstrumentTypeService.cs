using eNote.Application.Common.Crud;
using eNote.Application.Constants;
using System.Linq.Expressions;

namespace eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;

public sealed class InstrumentTypeService(IAppDbContext context) : ReferenceDataCrudService<InstrumentType, InstrumentTypeDto, InstrumentTypeRequest, InstrumentTypeSearchObject>(context)
{
    protected override Expression<Func<InstrumentType, InstrumentTypeDto>> Projection => x => new InstrumentTypeDto
    {
        Id = x.Id,
        Type = x.Type,
        MonthlyFee = x.MonthlyFee,
        DeleteBlockedReason = Db.Set<Instrument>().IgnoreQueryFilters().Any(i => i.InstrumentTypeId == x.Id)
            ? Messages.InstrumentTypeDeleteBlocked
            : null
    };

    protected override (string IndexName, string Message)? UniqueConflict =>
        (DbConstraintNames.InstrumentTypeTypeUniqueIndex, Messages.InstrumentTypeAlreadyExists);

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
        var reason = await Db.Set<InstrumentType>()
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
