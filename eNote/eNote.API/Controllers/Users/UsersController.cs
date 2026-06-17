using eNote.API.Controllers.Base;
using eNote.Application.Features.Users;
using eNote.Application.Features.Users.Services;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Users
{
    [Route("api/users")]
    public sealed class UsersController(IUserService userService) : CoreController
    {
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserProfileResponse>> GetCurrentUser()
        {
            UserProfileResponse? profile = await userService.GetCurrentUserAsync();

            if (profile is null)
            {
                return NotFound();
            }

            return Ok(profile);
        }

        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            (bool success, string? error) = await userService.UpdateProfileAsync(request);

            if (!success)
            {
                return BadRequest(new
                {
                    message = error
                });
            }

            return NoContent();
        }

        [HttpPut("me/password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            (bool success, string? error) = await userService.ChangePasswordAsync(request);

            if (!success)
            {
                return BadRequest(new
                {
                    message = error
                });
            }

            return NoContent();
        }
    }
}
