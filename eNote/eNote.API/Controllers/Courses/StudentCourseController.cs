using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Courses;
using eNote.Application.Features.Courses.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Courses
{
    [Authorize(Roles = AppRoles.Student)]
    [Route("api/student/courses")]
    public sealed class StudentCourseController(ICourseService service) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<CourseDto>>> GetPublished([FromQuery] CourseSearchObject search)
        {
            PagedResult<CourseDto> result = await service.GetPagedForStudentAsync(search);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CourseDto>> GetById(int id)
        {
            CourseDto dto = await service.GetByIdForStudentAsync(id);
            return Ok(dto);
        }

        [HttpPost("{id:int}/enroll")]
        public async Task<IActionResult> Enroll(int id)
        {
            await service.EnrollAsync(id);
            return NoContent();
        }

        [HttpPost("{id:int}/unenroll")]
        public async Task<IActionResult> Unenroll(int id)
        {
            await service.UnenrollAsync(id);
            return NoContent();
        }
    }
}
