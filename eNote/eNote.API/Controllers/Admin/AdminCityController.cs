using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.ReferenceData.Cities;
using eNote.Domain.Entities.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/v{version:apiVersion}/admin/cities")]
public sealed class AdminCityController(CityService service)
    : ReferenceDataController<City, CityDto, CityRequest, CitySearchObject>(service)
{
    protected override int GetId(CityDto dto) => dto.Id;
}
