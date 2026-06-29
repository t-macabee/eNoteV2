using eNote.API.Extensions;
using eNote.API.Controllers.Base;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace eNote.API.Controllers.Auth;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
public sealed class AuthController(IAuthService authService) : CoreController
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest model, CancellationToken cancellationToken)
    {
        AuthResponse response = await authService.LoginAsync(model, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest model, CancellationToken cancellationToken)
    {
        AuthResponse response = await authService.RegisterAsync(model, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        ForgotPasswordResponse response = await authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
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
}
