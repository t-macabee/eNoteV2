using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Courses;
using eNote.Application.Features.Courses.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Courses
{
    [Authorize(Roles = AppRoles.Instructor)]
    [Route("api/instructor/courses/{courseId:int}/ranking")]
    public sealed class InstructorRankingController(IRankingService rankingService) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CourseRankingEntryDto>>> GetRanking(int courseId)
        {
            return Ok(await rankingService.GetForInstructorAsync(courseId));
        }
    }
}
