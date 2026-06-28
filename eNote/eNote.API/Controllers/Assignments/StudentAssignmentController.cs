using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/assignments")]
public sealed class StudentAssignmentController(IAssignmentService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssignmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssignmentDto>>> GetMyAssignments([FromQuery] AssignmentSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetForStudentAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForStudentAsync(id, cancellationToken);
        return Ok(dto);
    }
}
