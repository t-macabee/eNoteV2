using eNote.Application.Common.Crud;
using eNote.Application.Constants;
using System.Linq.Expressions;

namespace eNote.Application.Features.ReferenceData.Addresses;

public sealed class AddressService(IAppDbContext context) : ReferenceDataCrudService<Address, AddressReferenceDto, AddressRequest, AddressSearchObject>(context)
{
    protected override Expression<Func<Address, AddressReferenceDto>> Projection => x => new AddressReferenceDto
    {
        Id = x.Id,
        CityId = x.CityId,
        City = x.City.Name,
        Street = x.Street,
        Number = x.Number,
        DeleteBlockedReason = Db.Set<Event>().Any(e => e.AddressId == x.Id)
            || Db.Set<MusicStore>().Any(s => s.AddressId == x.Id)
            ? Messages.AddressDeleteBlocked
            : null
    };

    protected override (string IndexName, string Message)? UniqueConflict =>
        (DbConstraintNames.AddressCityStreetNumberUniqueIndex, Messages.AddressAlreadyExists);

    protected override Address CreateEntity(AddressRequest request) => new()
    {
        CityId = request.CityId,
        Street = request.Street.Trim(),
        Number = request.Number.Trim()
    };

    protected override void UpdateEntity(Address entity, AddressRequest request)
    {
        entity.CityId = request.CityId;
        entity.Street = request.Street.Trim();
        entity.Number = request.Number.Trim();
    }

    public override async Task<AddressReferenceDto> CreateAsync(AddressRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCityExistsAsync(request.CityId, cancellationToken);
        return await base.CreateAsync(request, cancellationToken);
    }

    public override async Task<AddressReferenceDto> UpdateAsync(int id, AddressRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCityExistsAsync(request.CityId, cancellationToken);
        return await base.UpdateAsync(id, request, cancellationToken);
    }

    private async Task EnsureCityExistsAsync(int cityId, CancellationToken cancellationToken)
    {
        if (!await Db.Set<City>().AnyAsync(x => x.Id == cityId, cancellationToken))
        {
            throw new BusinessException(Messages.CityNotFound);
        }
    }

    protected override IQueryable<Address> ApplySearch(IQueryable<Address> query, AddressSearchObject search)
    {
        return query.ApplySearch(search);
    }

    protected override IOrderedQueryable<Address> ApplyDefaultOrder(IQueryable<Address> query)
    {
        return query.OrderBy(x => x.City.Name).ThenBy(x => x.Street);
    }

    protected override string NotFoundMessage => Messages.AddressNotFound;

    protected override async Task EnsureDeletableAsync(Address entity, CancellationToken ct)
    {
        var reason = await Db.Set<Address>()
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
