using eNote.Application.Common.Crud;
using eNote.Application.Constants;
using System.Linq.Expressions;

namespace eNote.Application.Features.ReferenceData.Cities;

public sealed class CityService(IAppDbContext context) : ReferenceDataCrudService<City, CityDto, CityRequest, CitySearchObject>(context)
{
    private static readonly Expression<Func<City, CityDto>> ProjectToDto = x => new CityDto
    {
        Id = x.Id,
        Name = x.Name,
        DeleteBlockedReason = x.Addresses.Any() ? Messages.CityDeleteBlocked : null
    };

    protected override Expression<Func<City, CityDto>> Projection => ProjectToDto;

    protected override (string IndexName, string Message)? UniqueConflict =>
        (DbConstraintNames.CityNameUniqueIndex, Messages.CityAlreadyExists);

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

    protected override string NotFoundMessage => Messages.CityNotFound;

    protected override async Task EnsureDeletableAsync(City entity, CancellationToken ct)
    {
        var reason = await Db.Set<City>()
            .AsNoTracking()
            .Where(x => x.Id == entity.Id)
            .Select(ProjectToDto)
            .Select(x => x.DeleteBlockedReason)
            .FirstOrDefaultAsync(ct);

        if (reason is not null)
        {
            throw new BusinessException(reason);
        }
    }
}
