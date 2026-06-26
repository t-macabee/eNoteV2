using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Courses;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/instructor/courses/{courseId:int}/ranking")]
public sealed class InstructorRankingController(IRankingService rankingService, IReportService reportService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CourseRankingEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseRankingEntryDto>>> GetRanking(int courseId)
    {
        return Ok(await rankingService.GetForInstructorAsync(courseId));
    }

    [HttpGet("report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRankingReport(int courseId, CancellationToken cancellationToken)
    {
        var pdf = await reportService.GenerateCourseRankingPdfAsync(courseId, cancellationToken);
        return File(pdf, "application/pdf", $"course-{courseId}-ranking.pdf");
    }
}
