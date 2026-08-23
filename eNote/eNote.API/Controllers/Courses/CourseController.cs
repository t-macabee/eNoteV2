using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Courses;

[Route("api/v{version:apiVersion}/instructor/courses")]
[Route("api/v{version:apiVersion}/student/courses")]
public sealed class CourseController(
    CourseService service,
    CourseEnrollmentService enrollmentService) : CoreController
{
    // ── Instructor actions ──────────────────────────────────────────

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/courses")]
    [ProducesResponseType(typeof(PagedResult<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CourseDto>>> GetMyCourses([FromQuery] CourseSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetPagedForInstructorAsync(search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/courses/{id:int}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CourseDto>> GetByIdForInstructor(int id, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForInstructorAsync(id, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPost("~/api/v{version:apiVersion}/instructor/courses")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CourseDto>> Create([FromBody] CourseRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdForInstructor), new { id = dto.Id }, dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPut("~/api/v{version:apiVersion}/instructor/courses/{id:int}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CourseDto>> Update(int id, [FromBody] CourseRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.UpdateAsync(id, request, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpDelete("~/api/v{version:apiVersion}/instructor/courses/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    // ── Student actions ─────────────────────────────────────────────

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("~/api/v{version:apiVersion}/student/courses")]
    [ProducesResponseType(typeof(PagedResult<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CourseDto>>> GetPublished([FromQuery] CourseSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetPagedForStudentAsync(search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("~/api/v{version:apiVersion}/student/courses/{id:int}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CourseDto>> GetByIdForStudent(int id, CancellationToken cancellationToken)
    {
        var dto = await service.GetByIdForStudentAsync(id, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpPost("~/api/v{version:apiVersion}/student/courses/{id:int}/enroll")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Enroll(int id, CancellationToken cancellationToken)
    {
        await enrollmentService.EnrollAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpPost("~/api/v{version:apiVersion}/student/courses/{id:int}/unenroll")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unenroll(int id, CancellationToken cancellationToken)
    {
        await enrollmentService.UnenrollAsync(id, cancellationToken);
        return NoContent();
    }
}
