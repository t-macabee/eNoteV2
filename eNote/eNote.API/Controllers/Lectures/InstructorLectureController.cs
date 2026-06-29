using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Application.Features.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Lectures;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/v{version:apiVersion}/instructor/lectures")]
public sealed class InstructorLectureController(ILectureService service, ILectureAttendanceService attendanceService, IReportService reportService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LectureDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LectureDto>>> GetMyLectures([FromQuery] LectureSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetPagedForInstructorAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForInstructorAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<LectureDto>> Create([FromBody] LectureCreateRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new
        {
            id = dto.Id
        }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureDto>> Update(int id, [FromBody] LectureUpdateRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.UpdateAsync(id, request, cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureDto>> Cancel(int id, CancellationToken cancellationToken)
    {
        var dto = await service.CancelAsync(id, cancellationToken);
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
    public async Task<ActionResult<PagedResult<AttendanceDto>>> GetAttendance(int id, [FromQuery] AttendanceSearchObject search, CancellationToken cancellationToken)
    {
        var result = await attendanceService.GetAttendanceAsync(id, search, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}/attendance")]
    [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceDto>> MarkAttendance(int id, [FromBody] MarkAttendanceRequest request, CancellationToken cancellationToken)
    {
        var dto = await attendanceService.MarkAttendanceAsync(id, request, cancellationToken);
        return Ok(dto);
    }
}
