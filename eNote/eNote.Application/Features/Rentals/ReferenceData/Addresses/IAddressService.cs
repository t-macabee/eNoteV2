using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Rentals.ReferenceData.Addresses;

public interface IAddressService
{
    Task<PagedResult<AddressReferenceDto>> GetPagedAsync(AddressSearchObject search, CancellationToken cancellationToken = default);
    Task<AddressReferenceDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AddressReferenceDto> CreateAsync(AddressRequest request, CancellationToken cancellationToken = default);
    Task<AddressReferenceDto> UpdateAsync(int id, AddressRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
