using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Assignments;
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
        [ProducesResponseType(typeof(AssignmentSubmissionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AssignmentSubmissionDto>> Submit(int id, IFormFile? file, CancellationToken ct)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(new { message = Messages.FileNotProvided });
            }

            await using Stream stream = file.OpenReadStream();
            var dto = await service.SubmitWithFileAsync(id, stream, file.FileName, file.ContentType, ct);
            return Ok(dto);
        }
    }
}
