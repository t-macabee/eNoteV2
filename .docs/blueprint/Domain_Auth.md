# Bounded Context: Auth
Total Files Contained: 13
---

## File: eNote\eNote.Application\Common\Exceptions\AuthenticationException.cs
```cs
namespace eNote.Application.Common.Exceptions;

public class AuthenticationException(string? message = null) : AppException(401, "error.unauthorized", message)
{
}

```

## File: eNote\eNote.Application\Common\Exceptions\AuthorizationException.cs
```cs
namespace eNote.Application.Common.Exceptions;

public class AuthorizationException(string? message = null) : AppException(403, "error.forbidden", message)
{
}

```

## File: eNote\eNote.Application\Features\Identity\Auth\AuthResponse.cs
```cs
using System.Text.Json.Serialization;

namespace eNote.Application.Features.Identity.Auth;

public sealed class AuthResponse
{
    [JsonPropertyName("userId")]
    public int UserId { get; init; }

    [JsonPropertyName("username")]
    public string Username { get; init; } = null!;

    [JsonPropertyName("roles")]
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    [JsonPropertyName("token")]
    public string Token { get; init; } = null!;
}

```

## File: eNote\eNote.Application\Features\Identity\Auth\ForgotPasswordRequest.cs
```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Identity.Auth;

public sealed class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

```

## File: eNote\eNote.Application\Features\Identity\Auth\ForgotPasswordResponse.cs
```cs
namespace eNote.Application.Features.Identity.Auth;

public sealed class ForgotPasswordResponse
{
    public string Message { get; init; } = null!;
}

```

## File: eNote\eNote.Application\Features\Identity\Auth\LoginRequest.cs
```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Identity.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Korisničko ime je obavezno.")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Lozinka je obavezna.")]
    public string Password { get; set; } = null!;
}

```

## File: eNote\eNote.Application\Features\Identity\Auth\RegisterRequest.cs
```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Identity.Auth;

public class RegisterRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

```

## File: eNote\eNote.Application\Features\Identity\Auth\ResetPasswordRequest.cs
```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Identity.Auth;

public sealed class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string NewPassword { get; set; } = string.Empty;
}

```

## File: eNote\eNote.Application\Features\Identity\Auth\Services\IAuthService.cs
```cs
using eNote.Application.Features.Identity.Auth;

namespace eNote.Application.Features.Identity.Auth.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest model);
    Task<AuthResponse> RegisterAsync(RegisterRequest model);
    Task LogoutAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}

```

## File: eNote\eNote.Application\Features\Identity\Auth\Services\ITokenRevocationService.cs
```cs
namespace eNote.Application.Features.Identity.Auth.Services;

public interface ITokenRevocationService
{
    Task RevokeAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default);
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
}

```

## File: eNote\eNote.Application\Features\Identity\Auth\Services\ITokenService.cs
```cs
namespace eNote.Application.Features.Identity.Auth.Services;

public interface ITokenService
{
    string GenerateToken(int userId, string username, IList<string> roles);
}

```

## File: eNote\eNote.Infrastructure\Identity\AuthService.cs
```cs
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Auth.Services;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eNote.Infrastructure.Identity;

public sealed class AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService, IUserProvisioningService userProvisioning, ITokenRevocationService tokenRevocationService, IEmailService emailService, IWebHostEnvironment environment, ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest model)
    {
        var username = model.Username.Trim();
        AppUser? user = await userManager.FindByNameAsync(username);

        if (user == null || !user.IsActive)
        {
            throw new AuthenticationException(Messages.InvalidCredentials);
        }

        SignInResult result = await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

        if (result.IsLockedOut || await userManager.IsLockedOutAsync(user))
        {
            throw new AuthenticationException(Messages.AccountLocked);
        }

        if (!result.Succeeded)
        {
            throw new AuthenticationException(Messages.InvalidCredentials);
        }

        IList<string> roles = await userManager.GetRolesAsync(user);

        if (roles.Count != 1)
        {
            throw new BusinessException(Messages.UserSingleRoleRequired);
        }

        var token = tokenService.GenerateToken(user.Id, user.UserName!, roles);

        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.UserName!,
            Roles = roles.ToList().AsReadOnly(),
            Token = token
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest model)
    {
        (_, var error) = await userProvisioning.RegisterStudentAsync(model);

        if (error is not null)
        {
            if (error == Messages.UsernameTaken || error == Messages.EmailTaken)
            {
                throw new ConflictException(Messages.UsernameTaken);
            }

            throw new BusinessException(error);
        }

        AppUser? user = await userManager.FindByNameAsync(model.Username.Trim());

        if (user is null)
        {
            throw new BusinessException(Messages.InternalError);
        }

        IList<string> roles = await userManager.GetRolesAsync(user);

        var token = tokenService.GenerateToken(user.Id, user.UserName!, roles);

        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.UserName!,
            Roles = roles.ToList().AsReadOnly(),
            Token = token
        };
    }

    public Task LogoutAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default) => tokenRevocationService.RevokeAsync(jti, expiresAtUtc, cancellationToken);

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        AppUser? user = await userManager.FindByEmailAsync(email);

        if (user is null || !user.IsActive)
        {
            return new ForgotPasswordResponse { Message = Messages.PasswordResetEmailSent };
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        await emailService.SendPasswordResetAsync(user.Email!, token, cancellationToken);

        if (environment.IsDevelopment())
        {
            logger.LogInformation("Password reset token for {Email}: {Token}", email, token);
        }

        return new ForgotPasswordResponse { Message = Messages.PasswordResetEmailSent };
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        AppUser? user = await userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null || !user.IsActive)
        {
            throw new BusinessException(Messages.PasswordResetFailed);
        }

        IdentityResult result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new BusinessException(Messages.PasswordResetFailed + " " + errors);
        }
    }
}

```

## File: eNote\eNote.API\Controllers\Auth\AuthController.cs
```cs
﻿using eNote.API.Extensions;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Auth.Services;
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

```

