namespace eNote.Application.Features.ReferenceData.Addresses;

public static class AddressExistenceExtensions
{
    public static async Task EnsureAddressExistsAsync(this IAppDbContext db, int? addressId, CancellationToken cancellationToken)
    {
        if (addressId is { } id && !await db.Set<Address>().AnyAsync(x => x.Id == id, cancellationToken))
        {
            throw new BusinessException(Messages.AddressNotFound);
        }
    }
}
