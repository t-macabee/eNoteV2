using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Lectures;
using eNote.Application.Features.Lectures.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Lectures
{
    [Authorize(Roles = AppRoles.Instructor)]
    [Route("api/instructor/lectures")]
    public sealed class InstructorLectureController(ILectureService service) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<LectureDto>>> GetMyLectures([FromQuery] LectureSearchObject search)
        {
            PagedResult<LectureDto> result = await service.GetPagedForInstructorAsync(search);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<LectureDto>> GetById(int id)
        {
            LectureDto dto = await service.GetByIdForInstructorAsync(id);
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<LectureDto>> Create([FromBody] LectureCreateRequest request)
        {
            LectureDto dto = await service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new
            {
                id = dto.Id
            }, dto);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<LectureDto>> Update(int id, [FromBody] LectureUpdateRequest request)
        {
            LectureDto dto = await service.UpdateAsync(id, request);
            return Ok(dto);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await service.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<LectureDto>> Cancel(int id)
        {
            LectureDto dto = await service.CancelAsync(id);
            return Ok(dto);
        }

        [HttpGet("{id:int}/attendance")]
        public async Task<ActionResult<PagedResult<AttendanceDto>>> GetAttendance(int id, int page = 1, int pageSize = 20)
        {
            PagedResult<AttendanceDto> result = await service.GetAttendanceAsync(id, page, pageSize);
            return Ok(result);
        }

        [HttpPut("{id:int}/attendance")]
        public async Task<ActionResult<AttendanceDto>> MarkAttendance(int id, [FromBody] MarkAttendanceRequest request)
        {
            AttendanceDto dto = await service.MarkAttendanceAsync(id, request);
            return Ok(dto);
        }
    }
}
