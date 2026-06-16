using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Assignments;
using eNote.Application.Features.Assignments.Search;
using eNote.Application.Features.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments
{
    [Authorize(Roles = AppRoles.Student)]
    [Route("api/student/assignments")]
    public sealed class StudentAssignmentController(IAssignmentService service) : CoreController
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<AssignmentDto>>> GetMyAssignments([FromQuery] AssignmentSearchObject search)
        {
            var result = await service.GetForStudentAsync(search);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AssignmentDto>> GetById(int id)
        {
            var dto = await service.GetByIdForStudentAsync(id);
            return Ok(dto);
        }

        [HttpPost("{id:int}/submit")]
        public async Task<ActionResult<AssignmentSubmissionDto>> Submit(int id, [FromBody] AssignmentSubmitRequest request)
        {
            var dto = await service.SubmitAsync(id, request);
            return Ok(dto);
        }
    }
}
