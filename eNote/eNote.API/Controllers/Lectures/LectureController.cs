using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Application.Features.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Lectures;

[Route("api/v{version:apiVersion}/instructor/lectures")]
[Route("api/v{version:apiVersion}/student/lectures")]
public sealed class LectureController(
    LectureService service,
    LectureAttendanceService attendanceService,
    IReportService reportService) : CoreController
{
    // ── Instructor actions ──────────────────────────────────────────

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/lectures")]
    [ProducesResponseType(typeof(PagedResult<LectureDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LectureDto>>> GetMyLectures([FromQuery] LectureSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetPagedForInstructorAsync(search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/lectures/{id:int}")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureDto>> GetByIdForInstructor(int id, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForInstructorAsync(id, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPost("~/api/v{version:apiVersion}/instructor/lectures")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<LectureDto>> Create([FromBody] LectureCreateRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdForInstructor), new { id = dto.Id }, dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPut("~/api/v{version:apiVersion}/instructor/lectures/{id:int}")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureDto>> Update(int id, [FromBody] LectureUpdateRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.UpdateAsync(id, request, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpDelete("~/api/v{version:apiVersion}/instructor/lectures/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPost("~/api/v{version:apiVersion}/instructor/lectures/{id:int}/cancel")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureDto>> Cancel(int id, CancellationToken cancellationToken)
    {
        var dto = await service.CancelAsync(id, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/lectures/{id:int}/attendance/report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendanceReport(int id, CancellationToken cancellationToken)
    {
        var pdf = await reportService.GenerateLectureAttendancePdfAsync(id, cancellationToken);
        return File(pdf, "application/pdf", $"lecture-{id}-attendance.pdf");
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/lectures/{id:int}/attendance")]
    [ProducesResponseType(typeof(PagedResult<AttendanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AttendanceDto>>> GetAttendance(int id, [FromQuery] AttendanceSearchObject search, CancellationToken cancellationToken)
    {
        var result = await attendanceService.GetAttendanceAsync(id, search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPut("~/api/v{version:apiVersion}/instructor/lectures/{id:int}/attendance")]
    [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceDto>> MarkAttendance(int id, [FromBody] MarkAttendanceRequest request, CancellationToken cancellationToken)
    {
        var dto = await attendanceService.MarkAttendanceAsync(id, request, cancellationToken);
        return Ok(dto);
    }

    // ── Student actions ─────────────────────────────────────────────

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("~/api/v{version:apiVersion}/student/lectures")]
    [ProducesResponseType(typeof(PagedResult<LectureDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LectureDto>>> GetAvailable([FromQuery] LectureSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetPagedForStudentAsync(search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("~/api/v{version:apiVersion}/student/lectures/{id:int}")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureDto>> GetByIdForStudent(int id, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForStudentAsync(id, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpPost("~/api/v{version:apiVersion}/student/lectures/{id:int}/rsvp")]
    [ProducesResponseType(typeof(RsvpResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RsvpResponse>> Rsvp(int id, [FromBody] RsvpRequest request, CancellationToken cancellationToken)
    {
        var response = await attendanceService.RsvpAsync(id, request, cancellationToken);
        return Ok(response);
    }
}
