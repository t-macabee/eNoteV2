using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Assignments;
using eNote.Application.Features.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments
{
    [Authorize(Roles = AppRoles.Instructor)]
    [Route("api/instructor/lectures/{lectureId:int}/assignments")]
    public sealed class InstructorAssignmentController(IAssignmentService service) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<AssignmentDto>>> GetForLecture(int lectureId, [FromQuery] AssignmentSearchObject search)
        {
            var result = await service.GetForLectureAsync(lectureId, search);
            return Ok(result);
        }

        [HttpGet("{assignmentId:int}")]
        public async Task<ActionResult<AssignmentDto>> GetById(int lectureId, int assignmentId)
        {
            var dto = await service.GetByIdForInstructorAsync(lectureId, assignmentId);
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<AssignmentDto>> Create(int lectureId, [FromBody] AssignmentRequest request)
        {
            var dto = await service.CreateAsync(lectureId, request);
            return CreatedAtAction(nameof(GetById), new
            {
                lectureId,
                assignmentId = dto.Id
            }, dto);
        }

        [HttpPut("{assignmentId:int}")]
        public async Task<ActionResult<AssignmentDto>> Update(int lectureId, int assignmentId, [FromBody] AssignmentRequest request)
        {
            var dto = await service.UpdateAsync(lectureId, assignmentId, request);
            return Ok(dto);
        }

        [HttpDelete("{assignmentId:int}")]
        public async Task<IActionResult> Delete(int lectureId, int assignmentId)
        {
            await service.DeleteAsync(lectureId, assignmentId);
            return NoContent();
        }
    }
}
