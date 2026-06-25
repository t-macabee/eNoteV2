using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.LectureNotes;
using eNote.Application.Features.LectureNotes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.LectureNotes;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/instructor/lectures/{lectureId:int}/notes")]
public sealed class InstructorLectureNoteController(ILectureNoteService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LectureNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LectureNoteDto>>> GetForLecture(int lectureId, [FromQuery] LectureNoteSearchObject search)
    {
        var result = await service.GetForLectureAsync(lectureId, search);
        return Ok(result);
    }

    [HttpGet("{noteId:int}")]
    [ProducesResponseType(typeof(LectureNoteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureNoteDto>> GetById(int lectureId, int noteId)
    {
        var dto = await service.GetByIdForInstructorAsync(lectureId, noteId);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(LectureNoteDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<LectureNoteDto>> Create(int lectureId, [FromBody] LectureNoteRequest request)
    {
        var dto = await service.CreateAsync(lectureId, request);
        return CreatedAtAction(nameof(GetById), new
        {
            lectureId,
            noteId = dto.Id
        }, dto);
    }

    [HttpPut("{noteId:int}")]
    [ProducesResponseType(typeof(LectureNoteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureNoteDto>> Update(int lectureId, int noteId, [FromBody] LectureNoteRequest request)
    {
        var dto = await service.UpdateAsync(lectureId, noteId, request);
        return Ok(dto);
    }

    [HttpDelete("{noteId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int lectureId, int noteId)
    {
        await service.DeleteAsync(lectureId, noteId);
        return NoContent();
    }
}
