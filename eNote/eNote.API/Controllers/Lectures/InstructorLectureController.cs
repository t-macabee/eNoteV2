using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Lectures;
using eNote.Application.Features.Lectures.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Lectures
{
    [Authorize(Roles = AppRoles.Instructor)]
    [Route("api/instructor/lectures")]
    public sealed class InstructorLectureController(ILectureService service) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<LectureDto>>> GetMyLectures(int page = 1, int pageSize = 20)
        {
            var result = await service.GetPagedForInstructorAsync(CurrentUserId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<LectureDto>> GetById(int id)
        {
            var dto = await service.GetByIdForInstructorAsync(id, CurrentUserId);
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<LectureDto>> Create([FromBody] LectureCreateRequest request)
        {
            var dto = await service.CreateAsync(CurrentUserId, request);
            return Ok(dto);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<LectureDto>> Update(int id, [FromBody] LectureUpdateRequest request)
        {
            var dto = await service.UpdateAsync(id, CurrentUserId, request);
            return Ok(dto);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await service.DeleteAsync(id, CurrentUserId);
            return NoContent();
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<LectureDto>> Cancel(int id)
        {
            var dto = await service.CancelAsync(id, CurrentUserId);
            return Ok(dto);
        }

        [HttpGet("{id:int}/attendance")]
        public async Task<ActionResult<PagedResult<AttendanceDto>>> GetAttendance(int id, int page = 1, int pageSize = 20)
        {
            var result = await service.GetAttendanceAsync(id, CurrentUserId, page, pageSize);
            return Ok(result);
        }

        [HttpPut("{id:int}/attendance")]
        public async Task<ActionResult<AttendanceDto>> MarkAttendance(int id, [FromBody] MarkAttendanceRequest request)
        {
            var dto = await service.MarkAttendanceAsync(id, CurrentUserId, request);
            return Ok(dto);
        }
    }
}
