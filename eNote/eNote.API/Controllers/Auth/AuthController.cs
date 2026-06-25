using eNote.API.Extensions;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Features.Auth;
using eNote.Application.Features.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace eNote.API.Controllers.Auth;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest model)
    {
        AuthResponse response = await authService.LoginAsync(model);
        return Ok(response);
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest model)
    {
        AuthResponse response = await authService.RegisterAsync(model);
        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        ForgotPasswordResponse response = await authService.ForgotPasswordAsync(request, HttpContext.RequestAborted);
        return Ok(response);
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await authService.ResetPasswordAsync(request, HttpContext.RequestAborted);
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await authService.LogoutAsync(CurrentTokenJti, CurrentTokenExpiresAtUtc, HttpContext.RequestAborted);
        return NoContent();
    }

    private string CurrentTokenJti => User.FindFirstValue(JwtRegisteredClaimNames.Jti) ?? throw new AuthenticationException(Messages.InvalidUserClaim);

    private DateTime CurrentTokenExpiresAtUtc
    {
        get
        {
            var exp = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

            if (exp is null || !long.TryParse(exp, out var unixSeconds))
            {
                throw new AuthenticationException(Messages.InvalidUserClaim);
            }

            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
        }
    }
}
