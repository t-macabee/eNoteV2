using eNote.API.Controllers.Base;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Students;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Academic;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/v{version:apiVersion}/instructor/students")]
public sealed class InstructorStudentController(
    AdminStudentService studentService,
    IUserProvisioningService provisioningService,
    ICurrentUserContext currentUser,
    InstructorAccessService instructorAccess) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StudentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StudentDto>>> GetPaged(
        [FromQuery] StudentSearchObject search,
        CancellationToken cancellationToken)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);
        var result = await studentService.GetPagedForInstructorAsync(instructorId, search, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create(
        [FromBody] DelegatedUserCreateRequest request,
        CancellationToken cancellationToken)
    {
        (int userId, string? error) = await provisioningService.ProvisionStudentByInstructorAsync(request, cancellationToken);

        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return StatusCode(StatusCodes.Status201Created, new { userId });
    }
}
