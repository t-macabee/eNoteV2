using eNote.Application.Common.Crud;
using eNote.Application.Constants;
using System.Linq.Expressions;

namespace eNote.Application.Features.Rentals.ReferenceData.Manufacturers;

public sealed class ManufacturerService(IAppDbContext context) : ReferenceDataCrudService<Manufacturer, ManufacturerDto, ManufacturerRequest, ManufacturerSearchObject>(context)
{
    protected override Expression<Func<Manufacturer, ManufacturerDto>> Projection => x => new ManufacturerDto
    {
        Id = x.Id,
        Name = x.Name,
        DeleteBlockedReason = Db.Set<Instrument>().IgnoreQueryFilters().Any(i => i.ManufacturerId == x.Id)
            ? Messages.ManufacturerDeleteBlocked
            : null
    };

    protected override (string IndexName, string Message)? UniqueConflict =>
        (DbConstraintNames.ManufacturerNameUniqueIndex, Messages.ManufacturerAlreadyExists);

    protected override Manufacturer CreateEntity(ManufacturerRequest request) => new()
    {
        Name = request.Name.Trim()
    };

    protected override void UpdateEntity(Manufacturer entity, ManufacturerRequest request)
    {
        entity.Name = request.Name.Trim();
    }

    protected override IQueryable<Manufacturer> ApplySearch(IQueryable<Manufacturer> query, ManufacturerSearchObject search)
    {
        return query.ApplySearch(search);
    }

    protected override IOrderedQueryable<Manufacturer> ApplyDefaultOrder(IQueryable<Manufacturer> query)
    {
        return query.OrderBy(x => x.Name);
    }

    protected override string NotFoundMessage => Messages.ManufacturerNotFound;

    protected override async Task EnsureDeletableAsync(Manufacturer entity, CancellationToken ct)
    {
        var reason = await Db.Set<Manufacturer>()
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
