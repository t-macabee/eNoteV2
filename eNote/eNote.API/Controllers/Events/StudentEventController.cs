using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Communication.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Events;

[Authorize(Roles = AppRoles.Student)]
[Route("api/v{version:apiVersion}/student/events")]
public sealed class StudentEventController(EventService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetPagedForStudent([FromQuery] EventSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetPagedForStudentAsync(search, cancellationToken);
        return Ok(result);
    }
}
