using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Assignments;
using eNote.Application.Features.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Assignments;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/instructor/lectures/{lectureId:int}/assignments/{assignmentId:int}/submissions")]
public sealed class InstructorAssignmentSubmissionController(IAssignmentSubmissionService submissionService) : CoreController
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AssignmentSubmissionDto>>> GetSubmissions(int lectureId, int assignmentId, int page = 1, int pageSize = 20)
    {
        var result = await submissionService.GetSubmissionsAsync(lectureId, assignmentId, page, pageSize);
        return Ok(result);
    }

    [HttpPut("{submissionId:int}/grade")]
    public async Task<ActionResult<AssignmentSubmissionDto>> Grade(int lectureId, int assignmentId, int submissionId, [FromBody] GradeAssignmentRequest request)
    {
        var dto = await submissionService.GradeAsync(lectureId, assignmentId, submissionId, request);
        return Ok(dto);
    }
}
