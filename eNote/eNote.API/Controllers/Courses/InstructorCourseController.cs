using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Courses;
using eNote.Application.Features.Courses.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Courses
{
    [Authorize(Roles = AppRoles.Instructor)]
    [Route("api/instructor/courses")]
    public sealed class InstructorCourseController(ICourseService service) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<CourseDto>>> GetMyCourses(int page = 1, int pageSize = 20)
        {
            var result = await service.GetPagedForInstructorAsync(CurrentUserId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CourseDto>> GetById(int id)
        {
            var dto = await service.GetByIdForInstructorAsync(id, CurrentUserId);
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<CourseDto>> Create([FromBody] CourseCreateRequest request)
        {
            var dto = await service.CreateAsync(CurrentUserId, request);
            return Ok(dto);
        }
    }
}
