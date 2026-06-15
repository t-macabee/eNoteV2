using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Assignments;
using eNote.Application.Features.Assignments.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments
{
    [Authorize(Roles = AppRoles.Instructor)]
    [Route("api/instructor/lectures/{lectureId:int}/assignments")]
    public sealed class InstructorAssignmentController(IAssignmentService service) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<AssignmentDto>>> GetForLecture(int lectureId, int page = 1, int pageSize = 20)
        {
            var result = await service.GetForLectureAsync(lectureId, CurrentUserId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{assignmentId:int}")]
        public async Task<ActionResult<AssignmentDto>> GetById(int lectureId, int assignmentId)
        {
            var dto = await service.GetByIdForInstructorAsync(lectureId, assignmentId, CurrentUserId);
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<AssignmentDto>> Create(int lectureId, [FromBody] AssignmentCreateRequest request)
        {
            var dto = await service.CreateAsync(lectureId, CurrentUserId, request);
            return CreatedAtAction(nameof(GetById), new { lectureId, assignmentId = dto.Id }, dto);
        }

        [HttpPut("{assignmentId:int}")]
        public async Task<ActionResult<AssignmentDto>> Update(int lectureId, int assignmentId, [FromBody] AssignmentUpdateRequest request)
        {
            var dto = await service.UpdateAsync(lectureId, assignmentId, CurrentUserId, request);
            return Ok(dto);
        }

        [HttpDelete("{assignmentId:int}")]
        public async Task<IActionResult> Delete(int lectureId, int assignmentId)
        {
            await service.DeleteAsync(lectureId, assignmentId, CurrentUserId);
            return NoContent();
        }

        [HttpGet("{assignmentId:int}/submissions")]
        public async Task<ActionResult<PagedResult<AssignmentSubmissionDto>>> GetSubmissions(int lectureId, int assignmentId, int page = 1, int pageSize = 20)
        {
            var result = await service.GetSubmissionsAsync(lectureId, assignmentId, CurrentUserId, page, pageSize);
            return Ok(result);
        }

        [HttpPut("{assignmentId:int}/submissions/{submissionId:int}/grade")]
        public async Task<ActionResult<AssignmentSubmissionDto>> Grade(int lectureId, int assignmentId, int submissionId, [FromBody] GradeAssignmentRequest request)
        {
            var dto = await service.GradeAsync(lectureId, assignmentId, submissionId, CurrentUserId, request);
            return Ok(dto);
        }
    }
}
