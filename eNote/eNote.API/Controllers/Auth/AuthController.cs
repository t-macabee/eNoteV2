using eNote.API.Controllers.Base;
using eNote.API.Extensions;
using eNote.Application.Features.Auth;
using eNote.Application.Features.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace eNote.API.Controllers.Auth
{
    [Route("api/auth")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public class AuthController(IAuthService authService) : CoreController
    {
        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest model)
        {
            (AuthResponse? response, string? error) = await authService.Login(model);

            if (response is null)
            {
                return Unauthorized(new
                {
                    message = error
                });
            }

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest model)
        {
            (AuthResponse? response, string? error) = await authService.Register(model);

            if (response is null)
            {
                return BadRequest(new
                {
                    message = error
                });
            }

            return Ok(response);
        }

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout()
        {
            await authService.LogoutAsync(CurrentTokenJti, CurrentTokenExpiresAtUtc, HttpContext.RequestAborted);
            return NoContent();
        }
    }
}
