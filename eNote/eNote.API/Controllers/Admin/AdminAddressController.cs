using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.ReferenceData.Addresses;
using eNote.Domain.Entities.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/v{version:apiVersion}/admin/addresses")]
public sealed class AdminAddressController(AddressService service)
    : ReferenceDataController<Address, AddressReferenceDto, AddressRequest, AddressSearchObject>(service)
{
    protected override int GetId(AddressReferenceDto dto) => dto.Id;
}
