using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Courses;

public sealed class CourseController(
    CourseService service,
    CourseEnrollmentService enrollmentService,
    InstructorEnrollmentService instructorEnrollmentService) : CoreController
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
    [HttpGet("~/api/v{version:apiVersion}/instructor/courses/catalog")]
    [ProducesResponseType(typeof(PagedResult<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CourseDto>>> GetCatalog([FromQuery] CourseSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetPagedCatalogForInstructorAsync(search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/courses/catalog/{id:int}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CourseDto>> GetCatalogById(int id, CancellationToken cancellationToken)
    {
        var dto = await service.GetCatalogByIdForInstructorAsync(id, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/courses/catalog/instructors")]
    [ProducesResponseType(typeof(List<CourseCatalogInstructorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CourseCatalogInstructorDto>>> GetCatalogInstructors(CancellationToken cancellationToken)
    {
        var result = await service.GetCatalogInstructorsAsync(cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/courses/catalog/summary")]
    [ProducesResponseType(typeof(CourseCatalogSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CourseCatalogSummaryDto>> GetCatalogSummary(CancellationToken cancellationToken)
    {
        var result = await service.GetCatalogSummaryAsync(cancellationToken);
        return Ok(result);
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

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/enrollments")]
    [ProducesResponseType(typeof(PagedResult<CourseEnrollmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CourseEnrollmentDto>>> GetEnrollments(int courseId, [FromQuery] CourseEnrollmentSearchObject search, CancellationToken cancellationToken)
    {
        var result = await instructorEnrollmentService.GetForCourseAsync(courseId, search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPost("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/enrollments/{enrollmentId:int}/approve")]
    [ProducesResponseType(typeof(CourseEnrollmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseEnrollmentDto>> ApproveEnrollment(int courseId, int enrollmentId, CancellationToken cancellationToken)
    {
        var dto = await instructorEnrollmentService.ApproveAsync(courseId, enrollmentId, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPost("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/enrollments/{enrollmentId:int}/reject")]
    [ProducesResponseType(typeof(CourseEnrollmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseEnrollmentDto>> RejectEnrollment(int courseId, int enrollmentId, [FromBody] EnrollmentRejectRequest request, CancellationToken cancellationToken)
    {
        var dto = await instructorEnrollmentService.RejectAsync(courseId, enrollmentId, request, cancellationToken);
        return Ok(dto);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPost("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/enrollments/{enrollmentId:int}/complete")]
    [ProducesResponseType(typeof(CourseEnrollmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseEnrollmentDto>> CompleteEnrollment(int courseId, int enrollmentId, CancellationToken cancellationToken)
    {
        var dto = await instructorEnrollmentService.CompleteAsync(courseId, enrollmentId, cancellationToken);
        return Ok(dto);
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
