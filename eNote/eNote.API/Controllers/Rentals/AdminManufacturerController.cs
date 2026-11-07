using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.ReferenceData.Manufacturers;
using eNote.Domain.Entities.Rentals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Rentals;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/v{version:apiVersion}/admin/manufacturers")]
public sealed class AdminManufacturerController(ManufacturerService service)
    : ReferenceDataController<Manufacturer, ManufacturerDto, ManufacturerRequest, ManufacturerSearchObject>(service)
{
    protected override int GetId(ManufacturerDto dto) => dto.Id;
}
