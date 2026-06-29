using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/assignments")]
public sealed class InstructorAssignmentController(IAssignmentService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssignmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssignmentDto>>> GetForLecture(int lectureId, [FromQuery] AssignmentSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetForLectureAsync(lectureId, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{assignmentId:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDto>> GetById(int lectureId, int assignmentId, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForInstructorAsync(lectureId, assignmentId, cancellationToken);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AssignmentDto>> Create(int lectureId, [FromBody] AssignmentRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.CreateAsync(lectureId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new
        {
            lectureId,
            assignmentId = dto.Id
        }, dto);
    }

    [HttpPut("{assignmentId:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDto>> Update(int lectureId, int assignmentId, [FromBody] AssignmentRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.UpdateAsync(lectureId, assignmentId, request, cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("{assignmentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int lectureId, int assignmentId, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(lectureId, assignmentId, cancellationToken);
        return NoContent();
    }
}
