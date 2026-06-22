using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Users;
using eNote.Application.Features.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin
{
    [Authorize(Roles = AppRoles.Administrator)]
    [Route("api/admin/users")]
    public sealed class AdminUsersController(
        IUserProfileService profileService,
        IUserProvisioningService provisioningService) : CoreController
    {
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserProfileResponse>> GetById(int id)
        {
            var profile = await profileService.GetUserAsync(id);

            if (profile is null)
            {
                return NotFound();
            }

            return Ok(profile);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Provision([FromBody] UserProvisionRequest request)
        {
            (var userId, var error) = await provisioningService.ProvisionUserAsync(request);

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
        public async Task<IActionResult> UpdateMembership(int id, [FromBody] UpdateMembershipRequest request)
        {
            await provisioningService.UpdateMembershipAsync(id, request);
            return NoContent();
        }
    }
}
