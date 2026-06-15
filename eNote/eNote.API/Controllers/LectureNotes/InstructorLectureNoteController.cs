using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.LectureNotes;
using eNote.Application.Features.LectureNotes.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.LectureNotes
{
    [Authorize(Roles = AppRoles.Instructor)]
    [Route("api/instructor/lectures/{lectureId:int}/notes")]
    public sealed class InstructorLectureNoteController(ILectureNoteService service) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<LectureNoteDto>>> GetForLecture(int lectureId, int page = 1, int pageSize = 20)
        {
            var result = await service.GetForLectureAsync(lectureId, CurrentUserId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{noteId:int}")]
        public async Task<ActionResult<LectureNoteDto>> GetById(int lectureId, int noteId)
        {
            var dto = await service.GetByIdForInstructorAsync(lectureId, noteId, CurrentUserId);
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<LectureNoteDto>> Create(int lectureId, [FromBody] LectureNoteCreateRequest request)
        {
            var dto = await service.CreateAsync(lectureId, CurrentUserId, request);
            return CreatedAtAction(nameof(GetById), new { lectureId, noteId = dto.Id }, dto);
        }

        [HttpPut("{noteId:int}")]
        public async Task<ActionResult<LectureNoteDto>> Update(int lectureId, int noteId, [FromBody] LectureNoteUpdateRequest request)
        {
            var dto = await service.UpdateAsync(lectureId, noteId, CurrentUserId, request);
            return Ok(dto);
        }

        [HttpDelete("{noteId:int}")]
        public async Task<IActionResult> Delete(int lectureId, int noteId)
        {
            await service.DeleteAsync(lectureId, noteId, CurrentUserId);
            return NoContent();
        }
    }
}
