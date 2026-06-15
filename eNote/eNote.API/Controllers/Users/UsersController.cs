using eNote.API.Controllers.Base;
using eNote.Application.Features.Users;
using eNote.Application.Features.Users.Services.Interfaces;
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
            var profile = await userService.GetCurrentUserAsync();

            if (profile is null)
                return NotFound();

            return Ok(profile);
        }
    }
}
