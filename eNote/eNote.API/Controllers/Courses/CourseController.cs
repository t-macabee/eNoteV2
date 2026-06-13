using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Courses;
using eNote.Application.Features.Courses.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Courses
{
    [Route("api/courses")]
    public sealed class CourseController(ICourseService service) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<CourseDto>>> GetAll(int page = 1, int pageSize = 20)
        {
            var result = await service.GetPagedAsync(page, pageSize, CurrentUserId);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CourseDto>> GetById(int id)
        {
            var dto = await service.GetByIdAsync(id, CurrentUserId);
            return Ok(dto);
        }

        [Authorize(Roles = AppRoles.Instructor)]
        [HttpPost]
        public async Task<ActionResult<CourseDto>> Create([FromBody] CourseCreateRequest request)
        {
            var dto = await service.CreateAsync(CurrentUserId, request);
            return Ok(dto);
        }

        [Authorize(Roles = AppRoles.Student)]
        [HttpPost("{id:int}/enroll")]
        public async Task<ActionResult> Enroll(int id)
        {
            await service.EnrollAsync(id, CurrentUserId);
            return Ok();
        }
    }
}
