using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Employees;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Shop;

[Authorize(Roles = AppRoles.StoreEmployee)]
[Route("api/v{version:apiVersion}/shop/employees")]
public sealed class ShopEmployeeController(
    ShopEmployeeService employeeService,
    IUserProvisioningService provisioningService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ShopEmployeeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ShopEmployeeDto>>> GetPaged(
        [FromQuery] ShopEmployeeSearchObject search,
        CancellationToken cancellationToken)
    {
        var result = await employeeService.GetPagedForCurrentStoreAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> Create(
        [FromBody] DelegatedUserCreateRequest request,
        CancellationToken cancellationToken)
    {
        (int userId, string? error) = await provisioningService.ProvisionEmployeeByManagerAsync(request, cancellationToken);

        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        return StatusCode(StatusCodes.Status201Created, new { userId });
    }

    [HttpPut("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(int id, [FromBody] UserStatusRequest request, CancellationToken cancellationToken)
    {
        var (success, error) = await employeeService.SetEmployeeActiveByManagerAsync(id, request.IsActive, cancellationToken);
        if (!success)
        {
            return BadRequest(new { message = error });
        }
        return NoContent();
    }
}
