using eNote.Application.Common.Paging;

namespace eNote.Application.Features.ReferenceData.Addresses;

public interface IAddressService
{
    Task<PagedResult<AddressReferenceDto>> GetPagedAsync(AddressSearchObject search);
    Task<AddressReferenceDto> GetByIdAsync(int id);
    Task<AddressReferenceDto> CreateAsync(AddressRequest request);
    Task<AddressReferenceDto> UpdateAsync(int id, AddressRequest request);
    Task DeleteAsync(int id);
}
