using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments;

[Route("api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/assignments")]
[Route("api/v{version:apiVersion}/student/assignments")]
public sealed class AssignmentController(AssignmentService service) : CoreController
{
    // ── Instructor actions ──────────────────────────────────────────

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/assignments")]
    [ProducesResponseType(typeof(PagedResult<AssignmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssignmentDto>>> GetForLecture(int lectureId, [FromQuery] AssignmentSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetForLectureAsync(lectureId, search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/assignments/{assignmentId:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDto>> GetByIdForInstructor(int lectureId, int assignmentId, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForInstructorAsync(lectureId, assignmentId, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPost("~/api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/assignments")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AssignmentDto>> Create(int lectureId, [FromBody] AssignmentRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.CreateAsync(lectureId, request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdForInstructor), new { lectureId, assignmentId = dto.Id }, dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPut("~/api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/assignments/{assignmentId:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDto>> Update(int lectureId, int assignmentId, [FromBody] AssignmentRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.UpdateAsync(lectureId, assignmentId, request, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpDelete("~/api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/assignments/{assignmentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int lectureId, int assignmentId, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(lectureId, assignmentId, cancellationToken);
        return NoContent();
    }

    // ── Student actions ─────────────────────────────────────────────

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("~/api/v{version:apiVersion}/student/assignments")]
    [ProducesResponseType(typeof(PagedResult<AssignmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssignmentDto>>> GetMyAssignments([FromQuery] AssignmentSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetForStudentAsync(search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("~/api/v{version:apiVersion}/student/assignments/{id:int}")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentDto>> GetByIdForStudent(int id, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForStudentAsync(id, cancellationToken);
        return Ok(dto);
    }
}
