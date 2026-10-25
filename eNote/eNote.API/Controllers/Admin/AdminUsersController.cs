using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/v{version:apiVersion}/admin/users")]
public sealed class AdminUsersController(
    UserProfileService profileService,
    IUserProvisioningService provisioningService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminUserListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminUserListItemDto>>> GetPaged([FromQuery] AdminUserSearchObject search, CancellationToken cancellationToken)
    {
        var result = await profileService.GetPagedAsync(search, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var profile = await profileService.GetUserAsync(id, true, cancellationToken);

        return Ok(profile);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Provision([FromBody] UserProvisionRequest request, CancellationToken cancellationToken)
    {
        var userId = await provisioningService.ProvisionUserAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new
        {
            id = userId
        }, new
        {
            userId
        });
    }

    [HttpPut("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetUserStatus(int id, [FromBody] UserStatusRequest request, CancellationToken cancellationToken)
    {
        await provisioningService.SetUserActiveAsync(id, request.IsActive, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        await provisioningService.DeleteUserAsync(id, cancellationToken);

        return NoContent();
    }
}
