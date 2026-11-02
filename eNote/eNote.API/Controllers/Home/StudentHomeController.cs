using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Home;
using eNote.Application.Features.Home.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Home;

public sealed class StudentHomeController(StudentHomeService service) : CoreController
{
    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("~/api/v{version:apiVersion}/student/home")]
    [ProducesResponseType(typeof(StudentHomeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentHomeDto>> Get(CancellationToken cancellationToken)
    {
        var dto = await service.GetAsync(cancellationToken);
        return Ok(dto);
    }
}
