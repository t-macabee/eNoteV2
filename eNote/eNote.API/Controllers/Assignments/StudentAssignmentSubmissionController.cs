using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments;

[Authorize(Roles = AppRoles.Student)]
[Route("api/v{version:apiVersion}/student/assignments/{id:int}/submit")]
public sealed class StudentAssignmentSubmissionController(IAssignmentSubmissionService submissionService) : CoreController
{
    [HttpPost]
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
