using eNote.Application.Features.Rentals.ReferenceData;

namespace eNote.Application.Features.Rentals.ReferenceData.Addresses;

public interface IAddressService : IReferenceCrudService<AddressReferenceDto, AddressRequest, AddressSearchObject>
{
}
