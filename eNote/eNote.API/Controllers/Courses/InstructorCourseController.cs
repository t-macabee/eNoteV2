using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Courses;
using eNote.Application.Features.Courses.Services;
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
            var result = await service.GetPagedForInstructorAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CourseDto>> GetById(int id)
        {
            var dto = await service.GetByIdForInstructorAsync(id);
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<CourseDto>> Create([FromBody] CourseRequest request)
        {
            var dto = await service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CourseDto>> Update(int id, [FromBody] CourseRequest request)
        {
            var dto = await service.UpdateAsync(id, request);
            return Ok(dto);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await service.DeleteAsync(id);
            return NoContent();
        }
    }
}
