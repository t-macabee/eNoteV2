using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/v{version:apiVersion}/admin/employees")]
public sealed class AdminEmployeeController(ShopEmployeeService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ShopEmployeeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ShopEmployeeDto>>> GetPaged(
        [FromQuery] ShopEmployeeSearchObject search,
        CancellationToken cancellationToken)
    {
        PagedResult<ShopEmployeeDto> result = await service.GetPagedAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ShopEmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShopEmployeeDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        ShopEmployeeDto dto = await service.GetByIdAsync(id, cancellationToken);
        return Ok(dto);
    }
}
