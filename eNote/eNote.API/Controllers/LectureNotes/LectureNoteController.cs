using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.LectureNotes;
using eNote.Application.Features.Academic.LectureNotes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.LectureNotes;

[Route("api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/notes")]
[Route("api/v{version:apiVersion}/student/lectures/{lectureId:int}/notes")]
public sealed class LectureNoteController(LectureNoteService service) : CoreController
{
    // ── Instructor actions ──────────────────────────────────────────

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/notes")]
    [ProducesResponseType(typeof(PagedResult<LectureNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LectureNoteDto>>> GetForLectureAsInstructor(int lectureId, [FromQuery] LectureNoteSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetForLectureAsync(lectureId, search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/notes/{noteId:int}")]
    [ProducesResponseType(typeof(LectureNoteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureNoteDto>> GetByIdForInstructor(int lectureId, int noteId, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForInstructorAsync(lectureId, noteId, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPost("~/api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/notes")]
    [ProducesResponseType(typeof(LectureNoteDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<LectureNoteDto>> Create(int lectureId, [FromBody] LectureNoteRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.CreateAsync(lectureId, request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdForInstructor), new { lectureId, noteId = dto.Id }, dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPut("~/api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/notes/{noteId:int}")]
    [ProducesResponseType(typeof(LectureNoteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureNoteDto>> Update(int lectureId, int noteId, [FromBody] LectureNoteRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.UpdateAsync(lectureId, noteId, request, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpDelete("~/api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/notes/{noteId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int lectureId, int noteId, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(lectureId, noteId, cancellationToken);
        return NoContent();
    }

    // ── Student actions ─────────────────────────────────────────────

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("~/api/v{version:apiVersion}/student/lectures/{lectureId:int}/notes")]
    [ProducesResponseType(typeof(PagedResult<LectureNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LectureNoteDto>>> GetForLectureAsStudent(int lectureId, [FromQuery] LectureNoteSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetForStudentAsync(lectureId, search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("~/api/v{version:apiVersion}/student/lectures/{lectureId:int}/notes/{noteId:int}")]
    [ProducesResponseType(typeof(LectureNoteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureNoteDto>> GetByIdForStudent(int lectureId, int noteId, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForStudentAsync(lectureId, noteId, cancellationToken);
        return Ok(dto);
    }
}
