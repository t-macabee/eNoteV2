using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Courses;

[Route("api/v{version:apiVersion}/instructor/courses/{courseId:int}/ranking")]
[Route("api/v{version:apiVersion}/student/courses/{courseId:int}/ranking")]
public sealed class RankingController(RankingService rankingService, IReportService reportService) : CoreController
{
    // ── Instructor actions ──────────────────────────────────────────

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/ranking")]
    [ProducesResponseType(typeof(IReadOnlyList<CourseRankingEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseRankingEntryDto>>> GetRankingForInstructor(int courseId, CancellationToken cancellationToken)
    {
        return Ok(await rankingService.GetForInstructorAsync(courseId, cancellationToken));
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/courses/{courseId:int}/ranking/report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRankingReport(int courseId, CancellationToken cancellationToken)
    {
        var pdf = await reportService.GenerateCourseRankingPdfAsync(courseId, cancellationToken);
        return File(pdf, "application/pdf", $"course-{courseId}-ranking.pdf");
    }

    // ── Student actions ─────────────────────────────────────────────

    [Authorize(Roles = AppRoles.Student)]
    [HttpGet("~/api/v{version:apiVersion}/student/courses/{courseId:int}/ranking")]
    [ProducesResponseType(typeof(IReadOnlyList<CourseRankingEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseRankingEntryDto>>> GetRankingForStudent(int courseId, CancellationToken cancellationToken)
    {
        return Ok(await rankingService.GetForStudentAsync(courseId, cancellationToken));
    }
}
