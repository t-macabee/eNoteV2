using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Application.Features.Rentals.ReferenceData.Addresses;

public sealed class AddressService(IAppDbContext context, IUserAccountService accountService) : ReferenceCrudService<Address, AddressReferenceDto, AddressRequest, AddressSearchObject>(context), IAddressService
{
    protected override string NotFoundMessage => Messages.AddressNotFound;

    protected override AddressReferenceDto Map(Address entity) => new()
    {
        Id = entity.Id,
        City = entity.City,
        Street = entity.Street,
        Number = entity.Number
    };

    protected override Address CreateEntity(AddressRequest request) => new()
    {
        City = request.City.Trim(),
        Street = request.Street.Trim(),
        Number = request.Number.Trim()
    };

    protected override void ApplyUpdate(Address entity, AddressRequest request)
    {
        entity.City = request.City.Trim();
        entity.Street = request.Street.Trim();
        entity.Number = request.Number.Trim();
    }

    protected override IQueryable<Address> ApplySearch(IQueryable<Address> query, AddressSearchObject search) => query.ApplySearch(search);
    protected override IOrderedQueryable<Address> Order(IQueryable<Address> query) => query.OrderBy(x => x.City).ThenBy(x => x.Street);

    protected override async Task EnsureDeletableAsync(Address entity, CancellationToken ct = default)
    {
        if (await accountService.IsAddressInUseAsync(entity.Id, ct))
        {
            throw new BusinessException(Messages.AddressDeleteBlocked);
        }
    }
}
