using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Instructors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/admin/instructors")]
public sealed class AdminInstructorController(IAdminInstructorService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InstructorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstructorDto>>> GetPaged([FromQuery] InstructorSearchObject search)
    {
        PagedResult<InstructorDto> result = await service.GetPagedAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstructorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstructorDto>> GetById(int id)
    {
        InstructorDto dto = await service.GetByIdAsync(id);
        return Ok(dto);
    }
}
