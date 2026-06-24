using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.ReferenceData.Addresses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/admin/addresses")]
public sealed class AdminAddressController(IAddressService service)
    : ReferenceCrudController<AddressReferenceDto, AddressRequest, AddressSearchObject>(service)
{
}
