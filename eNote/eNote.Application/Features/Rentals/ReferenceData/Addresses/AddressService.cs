using eNote.Application.Common.Crud;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Shared;

namespace eNote.Application.Features.Rentals.ReferenceData.Addresses;

public sealed class AddressService(IAppDbContext context, IUserAccountService accountService) : ReferenceDataCrudService<Address, AddressReferenceDto, AddressRequest, AddressSearchObject>(context)
{
    private readonly IUserAccountService _accountService = accountService;

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

    protected override void UpdateEntity(Address entity, AddressRequest request)
    {
        entity.City = request.City.Trim();
        entity.Street = request.Street.Trim();
        entity.Number = request.Number.Trim();
    }

    protected override IQueryable<Address> ApplySearch(IQueryable<Address> query, AddressSearchObject search)
    {
        return query.ApplySearch(search);
    }

    protected override IOrderedQueryable<Address> ApplyDefaultOrder(IQueryable<Address> query)
    {
        return query.OrderBy(x => x.City).ThenBy(x => x.Street);
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
