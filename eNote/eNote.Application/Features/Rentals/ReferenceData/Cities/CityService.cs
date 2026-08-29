using eNote.Application.Common.Crud;
using eNote.Application.Common.Persistence;

namespace eNote.Application.Features.Rentals.ReferenceData.Cities;

public sealed class CityService(IAppDbContext context) : ReferenceDataCrudService<City, CityDto, CityRequest, CitySearchObject>(context)
{
    protected override CityDto Map(City entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name
    };

    protected override City CreateEntity(CityRequest request) => new()
    {
        Name = request.Name.Trim()
    };

    protected override void UpdateEntity(City entity, CityRequest request)
    {
        entity.Name = request.Name.Trim();
    }

    protected override IQueryable<City> ApplySearch(IQueryable<City> query, CitySearchObject search)
    {
        return query.ApplySearch(search);
    }

    protected override IOrderedQueryable<City> ApplyDefaultOrder(IQueryable<City> query)
    {
        return query.OrderBy(x => x.Name);
    }

    protected override string NotFoundMessage => "Grad nije pronađen.";

    protected override async Task EnsureDeletableAsync(City entity, CancellationToken ct)
    {
        if (await Db.Set<Address>().AnyAsync(x => x.CityId == entity.Id, ct))
        {
            throw new BusinessException("Grad se ne može obrisati jer je u upotrebi.");
        }
    }
}
