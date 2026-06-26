using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Courses;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/courses/{courseId:int}/ranking")]
public sealed class StudentRankingController(IRankingService rankingService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CourseRankingEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseRankingEntryDto>>> GetRanking(int courseId)
    {
        return Ok(await rankingService.GetForStudentAsync(courseId));
    }
}
