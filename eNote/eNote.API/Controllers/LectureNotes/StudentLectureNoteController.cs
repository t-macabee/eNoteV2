using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.LectureNotes;
using eNote.Application.Features.Academic.LectureNotes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.LectureNotes;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/lectures/{lectureId:int}/notes")]
public sealed class StudentLectureNoteController(ILectureNoteService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LectureNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LectureNoteDto>>> GetForLecture(int lectureId, [FromQuery] LectureNoteSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetForStudentAsync(lectureId, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{noteId:int}")]
    [ProducesResponseType(typeof(LectureNoteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureNoteDto>> GetById(int lectureId, int noteId, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForStudentAsync(lectureId, noteId, cancellationToken);
        return Ok(dto);
    }
}
