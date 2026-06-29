using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Courses;

[Authorize(Roles = AppRoles.Student)]
[Route("api/v{version:apiVersion}/student/courses")]
public sealed class StudentCourseController(
    ICourseService service,
    ICourseEnrollmentService enrollmentService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CourseDto>>> GetPublished([FromQuery] CourseSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetPagedForStudentAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CourseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForStudentAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:int}/enroll")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Enroll(int id, CancellationToken cancellationToken)
    {
        await enrollmentService.EnrollAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/unenroll")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unenroll(int id, CancellationToken cancellationToken)
    {
        await enrollmentService.UnenrollAsync(id, cancellationToken);
        return NoContent();
    }
}
