using eNote.Application.Common.Localization;
using eNote.Application.Features.Auth;
using eNote.Application.Features.Auth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace eNote.API.Controllers.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest model)
        {
            var (response, error) = await authService.Login(model);

            if (response is null)
                return Unauthorized(new { message = error });

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest model)
        {
            var (response, error) = await authService.Register(model);

            if (response is null)
                return BadRequest(new { message = error });

            return Ok(response);
        }

        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout()
        {
            var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
            var exp = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

            if (string.IsNullOrWhiteSpace(jti) || exp is null || !long.TryParse(exp, out var unixSeconds))
                return BadRequest(new { message = Messages.BadRequest });

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
            await authService.LogoutAsync(jti, expiresAt, HttpContext.RequestAborted);

            return NoContent();
        }
    }
}
