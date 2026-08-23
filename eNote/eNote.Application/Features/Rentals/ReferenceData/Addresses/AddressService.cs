using eNote.Application.Common.Paging;
using eNote.Application.Common.Search;
using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Application.Features.Rentals.ReferenceData.Addresses;

public sealed class AddressService(IAppDbContext context, IUserAccountService accountService) : IAddressService
{
    private IAppDbContext Db => context;

    public Task<PagedResult<AddressReferenceDto>> GetPagedAsync(AddressSearchObject search, CancellationToken cancellationToken = default) =>
        Db.Set<Address>().AsNoTracking()
            .ApplySearch(search)
            .ToPagedResultAsync(search, Map, q => q.OrderBy(x => x.City).ThenBy(x => x.Street), ct: cancellationToken);

    public async Task<AddressReferenceDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<Address>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.AddressNotFound);

        return Map(entity);
    }

    public async Task<AddressReferenceDto> CreateAsync(AddressRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Address
        {
            City = request.City.Trim(),
            Street = request.Street.Trim(),
            Number = request.Number.Trim()
        };

        Db.Set<Address>().Add(entity);
        await Db.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<AddressReferenceDto> UpdateAsync(int id, AddressRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<Address>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.AddressNotFound);

        entity.City = request.City.Trim();
        entity.Street = request.Street.Trim();
        entity.Number = request.Number.Trim();

        await Db.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Db.Set<Address>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.AddressNotFound);

        if (await accountService.IsAddressInUseAsync(entity.Id, cancellationToken))
        {
            throw new BusinessException(Messages.AddressDeleteBlocked);
        }

        Db.Set<Address>().Remove(entity);
        await Db.SaveChangesAsync(cancellationToken);
    }

    private static AddressReferenceDto Map(Address entity) => new()
    {
        Id = entity.Id,
        City = entity.City,
        Street = entity.Street,
        Number = entity.Number
    };
}
