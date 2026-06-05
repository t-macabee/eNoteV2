using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Features.Courses.Services;
using eNote.Application.Features.Courses;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Courses
{
    [Route("api/courses")]
    public sealed class CourseController(ICourseService service) : CoreController
    {
        private readonly ICourseService _service = service;

        [HttpGet]
        public async Task<ActionResult<PagedResult<CourseDto>>> GetAll(int page = 1, int pageSize = 20)
        {
            var result = await _service.GetPagedAsync(page, pageSize, CurrentUserId);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CourseDto>> GetById(int id)
        {
            var dto = await _service.GetByIdAsync(id, CurrentUserId);
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<CourseDto>> Create([FromBody] CourseCreateRequest request)
        {
            var dto = await _service.CreateAsync(CurrentUserId, request);
            return Ok(dto);
        }

        [HttpPost("{id:int}/enroll")]
        public async Task<ActionResult> Enroll(int id)
        {
            await _service.EnrollAsync(id, CurrentUserId);
            return Ok();
        }
    }
}
