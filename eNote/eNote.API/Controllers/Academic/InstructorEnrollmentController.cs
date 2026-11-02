using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Academic;

[Authorize(Roles = AppRoles.Instructor)]
public sealed class InstructorEnrollmentController(InstructorEnrollmentService service) : CoreController
{
    [HttpGet("~/api/v{version:apiVersion}/instructor/enrollments")]
    [ProducesResponseType(typeof(PagedResult<CourseEnrollmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CourseEnrollmentDto>>> GetPaged([FromQuery] InstructorEnrollmentSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetForInstructorAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/enrollments")]
    [ProducesResponseType(typeof(PagedResult<CourseEnrollmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CourseEnrollmentDto>>> GetEnrollments(int courseId, [FromQuery] CourseEnrollmentSearchObject search, CancellationToken cancellationToken)
    {
        var result = await service.GetForCourseAsync(courseId, search, cancellationToken);
        return Ok(result);
    }

    [HttpPost("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/enrollments/{enrollmentId:int}/approve")]
    [ProducesResponseType(typeof(CourseEnrollmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseEnrollmentDto>> ApproveEnrollment(int courseId, int enrollmentId, CancellationToken cancellationToken)
    {
        var dto = await service.ApproveAsync(courseId, enrollmentId, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/enrollments/{enrollmentId:int}/reject")]
    [ProducesResponseType(typeof(CourseEnrollmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseEnrollmentDto>> RejectEnrollment(int courseId, int enrollmentId, [FromBody] EnrollmentRejectRequest request, CancellationToken cancellationToken)
    {
        var dto = await service.RejectAsync(courseId, enrollmentId, request, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/enrollments/{enrollmentId:int}/complete")]
    [ProducesResponseType(typeof(CourseEnrollmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseEnrollmentDto>> CompleteEnrollment(int courseId, int enrollmentId, CancellationToken cancellationToken)
    {
        var dto = await service.CompleteAsync(courseId, enrollmentId, cancellationToken);
        return Ok(dto);
    }
}
