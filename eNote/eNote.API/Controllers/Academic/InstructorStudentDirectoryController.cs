using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.StudentDirectory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Academic;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/v{version:apiVersion}/instructor/student-directory")]
public sealed class InstructorStudentDirectoryController(
    StudentDirectoryService directoryService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StudentDirectoryEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StudentDirectoryEntryDto>>> GetPaged(
        [FromQuery] StudentDirectorySearchObject search,
        CancellationToken cancellationToken)
    {
        var result = await directoryService.GetPagedForCurrentInstructorAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudentProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentProfileDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await directoryService.GetProfileForCurrentInstructorAsync(id, cancellationToken);
        return Ok(dto);
    }
}
