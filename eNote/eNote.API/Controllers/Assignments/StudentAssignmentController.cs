using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Assignments;
using eNote.Application.Features.Assignments.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments
{
    [Authorize(Roles = AppRoles.Student)]
    [Route("api/student/assignments")]
    public sealed class StudentAssignmentController(IAssignmentService service) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<AssignmentDto>>> GetMyAssignments(int page = 1, int pageSize = 20)
        {
            var result = await service.GetForStudentAsync(CurrentUserId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AssignmentDto>> GetById(int id)
        {
            var dto = await service.GetByIdForStudentAsync(id, CurrentUserId);
            return Ok(dto);
        }

        [HttpPost("{id:int}/submit")]
        public async Task<ActionResult<AssignmentSubmissionDto>> Submit(int id, [FromBody] AssignmentSubmitRequest request)
        {
            var dto = await service.SubmitAsync(id, CurrentUserId, request);
            return Ok(dto);
        }
    }
}
