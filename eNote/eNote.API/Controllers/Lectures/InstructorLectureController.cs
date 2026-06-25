using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Lectures;
using eNote.Application.Features.Lectures.Services;
using eNote.Application.Features.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Lectures;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/instructor/lectures")]
public sealed class InstructorLectureController(
    ILectureService service,
    ILectureAttendanceService attendanceService,
    IReportService reportService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LectureDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LectureDto>>> GetMyLectures([FromQuery] LectureSearchObject search)
    {
        var result = await service.GetPagedForInstructorAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureDto>> GetById(int id)
    {
        var dto = await service.GetByIdForInstructorAsync(id);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<LectureDto>> Create([FromBody] LectureCreateRequest request)
    {
        var dto = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new
        {
            id = dto.Id
        }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureDto>> Update(int id, [FromBody] LectureUpdateRequest request)
    {
        var dto = await service.UpdateAsync(id, request);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureDto>> Cancel(int id)
    {
        var dto = await service.CancelAsync(id);
        return Ok(dto);
    }

    [HttpGet("{id:int}/attendance/report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendanceReport(int id, CancellationToken cancellationToken)
    {
        var pdf = await reportService.GenerateLectureAttendancePdfAsync(id, cancellationToken);
        return File(pdf, "application/pdf", $"lecture-{id}-attendance.pdf");
    }

    [HttpGet("{id:int}/attendance")]
    [ProducesResponseType(typeof(PagedResult<AttendanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AttendanceDto>>> GetAttendance(int id, [FromQuery] AttendanceSearchObject search)
    {
        var result = await attendanceService.GetAttendanceAsync(id, search);
        return Ok(result);
    }

    [HttpPut("{id:int}/attendance")]
    [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceDto>> MarkAttendance(int id, [FromBody] MarkAttendanceRequest request)
    {
        var dto = await attendanceService.MarkAttendanceAsync(id, request);
        return Ok(dto);
    }
}
