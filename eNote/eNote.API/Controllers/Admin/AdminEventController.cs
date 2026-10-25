using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Communication.Events;
using eNote.Domain.Entities.Communication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/v{version:apiVersion}/admin/events")]
public sealed class AdminEventController(EventService service)
    : ReferenceDataController<Event, EventDto, EventRequest, EventSearchObject>(service)
{
    protected override int GetId(EventDto dto) => dto.Id;
}
