using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments;

[Route("api/v{version:apiVersion}/assignments/submissions")]
public sealed class AssignmentSubmissionController(AssignmentSubmissionService submissionService) : CoreController
{
    // ── Instructor actions ──────────────────────────────────────────

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpGet("~/api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/assignments/{assignmentId:int}/submissions")]
    [ProducesResponseType(typeof(PagedResult<AssignmentSubmissionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssignmentSubmissionDto>>> GetSubmissions(int lectureId, int assignmentId, [FromQuery] SubmissionSearchObject search, CancellationToken cancellationToken)
    {
        var result = await submissionService.GetSubmissionsAsync(lectureId, assignmentId, search, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Instructor)]
    [HttpPut("~/api/v{version:apiVersion}/instructor/lectures/{lectureId:int}/assignments/{assignmentId:int}/submissions/{submissionId:int}/grade")]
    [ProducesResponseType(typeof(AssignmentSubmissionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AssignmentSubmissionDto>> Grade(int lectureId, int assignmentId, int submissionId, [FromBody] GradeAssignmentRequest request, CancellationToken cancellationToken)
    {
        var dto = await submissionService.GradeAsync(lectureId, assignmentId, submissionId, request, cancellationToken);
        return Ok(dto);
    }

    // ── Student actions ─────────────────────────────────────────────

    [Authorize(Roles = AppRoles.Student)]
    [HttpPost("~/api/v{version:apiVersion}/student/assignments/{id:int}/submit")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(typeof(AssignmentSubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentSubmissionDto>> Submit(int id, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = Messages.FileNotProvided });
        }

        await using Stream stream = file.OpenReadStream();
        var dto = await submissionService.SubmitWithFileAsync(id, stream, file.FileName, file.ContentType, ct);
        return Ok(dto);
    }
}
