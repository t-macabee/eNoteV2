using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/assignments/{assignmentId:int}/submissions")]
public sealed class InstructorAssignmentSubmissionController(AssignmentSubmissionService submissionService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssignmentSubmissionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssignmentSubmissionDto>>> GetSubmissions(int lectureId, int assignmentId, [FromQuery] SubmissionSearchObject search, CancellationToken cancellationToken)
    {
        var result = await submissionService.GetSubmissionsAsync(lectureId, assignmentId, search, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{submissionId:int}/grade")]
    [ProducesResponseType(typeof(AssignmentSubmissionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentSubmissionDto>> Grade(int lectureId, int assignmentId, int submissionId, [FromBody] GradeAssignmentRequest request, CancellationToken cancellationToken)
    {
        var dto = await submissionService.GradeAsync(lectureId, assignmentId, submissionId, request, cancellationToken);
        return Ok(dto);
    }
}
