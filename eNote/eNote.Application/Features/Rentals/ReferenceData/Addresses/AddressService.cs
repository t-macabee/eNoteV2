using eNote.Application.Common.Crud;
using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Application.Features.Rentals.ReferenceData.Addresses;

public sealed class AddressService(IAppDbContext context, IUserAccountService accountService) : ReferenceDataCrudService<Address, AddressReferenceDto, AddressRequest, AddressSearchObject>(context)
{
    private readonly IUserAccountService _accountService = accountService;

    protected override AddressReferenceDto Map(Address entity) => new()
    {
        Id = entity.Id,
        CityId = entity.CityId,
        City = entity.City.Name,
        Street = entity.Street,
        Number = entity.Number
    };

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

    public override async Task<PagedResult<AddressReferenceDto>> GetPagedAsync(AddressSearchObject search, CancellationToken cancellationToken = default)
    {
        IQueryable<Address> query = Db.Set<Address>().AsNoTracking().Include(a => a.City);
        query = ApplySearch(query, search);
        query = ApplyDefaultOrder(query);
        return await query.ToPagedResultAsync(search, Map, ct: cancellationToken);
    }

    public override async Task<AddressReferenceDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<Address>().AsNoTracking().Include(a => a.City).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);
        return Map(entity);
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
        if (await _accountService.IsAddressInUseAsync(entity.Id, ct))
        {
            throw new BusinessException(Messages.AddressDeleteBlocked);
        }
    }
}
