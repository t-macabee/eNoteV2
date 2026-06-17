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
    public sealed class AdminController(IUserService userService) : CoreController
    {
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserProfileResponse>> GetById(int id)
        {
            UserProfileResponse? profile = await userService.GetUserAsync(id);

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
            (int userId, string? error) = await userService.ProvisionUserAsync(request);

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
    }
}
