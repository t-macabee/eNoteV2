using eNote.API.Controllers.Base;
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
    IUserProvisioningService provisioningService,
    eNote.Application.Common.Interfaces.ICurrentUserContext currentUser) : CoreController
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var profile = await profileService.GetUserAsync(id, true, cancellationToken);

        if (profile is null)
        {
            return NotFound();
        }

        return Ok(profile);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Provision([FromBody] UserProvisionRequest request, CancellationToken cancellationToken)
    {
        (var userId, var error) = await provisioningService.ProvisionUserAsync(request, cancellationToken);

        if (error is not null)
        {
            return BadRequest(new
            {
                message = error
            });
        }

        return CreatedAtAction(nameof(GetById), new
        {
            id = userId
        }, new
        {
            userId
        });
    }

    [HttpPut("{id:int}/membership")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMembership(int id, [FromBody] UpdateMembershipRequest request, CancellationToken cancellationToken)
    {
        await provisioningService.UpdateMembershipAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetUserStatus(int id, [FromBody] UserStatusRequest request, CancellationToken cancellationToken)
    {
        if (id == currentUser.UserId)
        {
            return Conflict(new { message = "Cannot modify your own account status." });
        }

        (var success, var error) = await provisioningService.SetUserActiveAsync(id, request.IsActive, cancellationToken);

        if (!success)
        {
            return NotFound(new { message = error });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        if (id == currentUser.UserId)
        {
            return Conflict(new { message = "Cannot delete your own account." });
        }

        (var success, var error) = await provisioningService.DeleteUserAsync(id, cancellationToken);

        if (!success)
        {
            if (error == eNote.Application.Common.Localization.Messages.UserDeleteBlocked)
            {
                return Conflict(new { message = error });
            }
            return NotFound(new { message = error });
        }

        return NoContent();
    }
}

public record UserStatusRequest(bool IsActive);
