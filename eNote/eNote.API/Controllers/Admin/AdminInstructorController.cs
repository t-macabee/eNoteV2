using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Instructors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/v{version:apiVersion}/admin/instructors")]
public sealed class AdminInstructorController(IAdminInstructorService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InstructorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstructorDto>>> GetPaged([FromQuery] InstructorSearchObject search, CancellationToken cancellationToken)
    {
        PagedResult<InstructorDto> result = await service.GetPagedAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstructorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstructorDto>> GetById(int id, CancellationToken cancellationToken)
    {
        InstructorDto dto = await service.GetByIdAsync(id, cancellationToken);
        return Ok(dto);
    }
}
