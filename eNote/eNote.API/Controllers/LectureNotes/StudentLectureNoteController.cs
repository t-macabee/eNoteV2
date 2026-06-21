using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.LectureNotes;
using eNote.Application.Features.LectureNotes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.LectureNotes
{
    [Authorize(Roles = AppRoles.Student)]
    [Route("api/student/lectures/{lectureId:int}/notes")]
    public sealed class StudentLectureNoteController(ILectureNoteService service) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<LectureNoteDto>>> GetForLecture(int lectureId, [FromQuery] LectureNoteSearchObject search)
        {
            var result = await service.GetForStudentAsync(lectureId, search);
            return Ok(result);
        }

        [HttpGet("{noteId:int}")]
        public async Task<ActionResult<LectureNoteDto>> GetById(int lectureId, int noteId)
        {
            var dto = await service.GetByIdForStudentAsync(lectureId, noteId);
            return Ok(dto);
        }
    }
}
