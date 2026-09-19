using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;
using eNote.Domain.Entities.Rentals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/v{version:apiVersion}/admin/instrument-types")]
public sealed class AdminInstrumentTypeController(InstrumentTypeService service)
    : ReferenceDataController<InstrumentType, InstrumentTypeDto, InstrumentTypeRequest, InstrumentTypeSearchObject>(service)
{
    protected override int GetId(InstrumentTypeDto dto) => dto.Id;
}
