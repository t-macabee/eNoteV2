using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Academic.Lectures.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Lectures;

[Authorize(Roles = AppRoles.Student)]
[Route("api/v{version:apiVersion}/student/lectures")]
public sealed class StudentLectureController(
    ILectureService service,
    ILectureAttendanceService attendanceService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LectureDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LectureDto>>> GetAvailable([FromQuery] LectureSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetPagedForStudentAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForStudentAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:int}/rsvp")]
    [ProducesResponseType(typeof(RsvpResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RsvpResponse>> Rsvp(int id, [FromBody] RsvpRequest request, CancellationToken cancellationToken)
    {
        var response = await attendanceService.RsvpAsync(id, request, cancellationToken);
        return Ok(response);
    }
}
