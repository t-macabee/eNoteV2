using eNote.Application.Common.Crud;
using eNote.Application.Constants;

namespace eNote.Application.Features.Rentals.ReferenceData.Manufacturers;

public sealed class ManufacturerService(IAppDbContext context) : ReferenceDataCrudService<Manufacturer, ManufacturerDto, ManufacturerRequest, ManufacturerSearchObject>(context)
{
    protected override (string IndexName, string Message)? UniqueConflict =>
        (DbConstraintNames.ManufacturerNameUniqueIndex, Messages.ManufacturerAlreadyExists);

    protected override ManufacturerDto Map(Manufacturer entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name
    };

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
        if (await Db.Set<Instrument>().IgnoreQueryFilters().AnyAsync(x => x.ManufacturerId == entity.Id, ct))
        {
            throw new BusinessException(Messages.ManufacturerDeleteBlocked);
        }
    }
}
