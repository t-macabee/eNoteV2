# Bounded Context: Auth

**Generated**: 2026-06-28T09:24:40.232209+00:00  
**Commit**: latest  
**Total Files**: 13

---

## 🤖 Agent Briefing (Read First)

This file contains the complete source for the **Auth** bounded context.

**Your goals when reading this context:**
1. Build an accurate mental model of entities, behavior, and state transitions.
2. Identify cross-context interactions (see "Key Interactions" sections).
3. Note any architectural smells, duplicated logic, or unnecessary abstractions.
4. Track how this context communicates with others (especially via events).

**Focus areas for deep analysis:**
- Domain entities with rich behavior (not anemic)
- Service orchestration and access control
- State machines / workflow logic
- Cross-domain event contracts

---

## File: `eNote\eNote.API\Controllers\Auth\AuthController.cs`
**Hash**: `b5bd18a68be6` | **Size**: 2510 chars

**Classes**: AuthController
```cs
﻿using eNote.API.Extensions;
using eNote.API.Controllers.Base;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace eNote.API.Controllers.Auth;

[ApiController]
[Route("api/auth")]
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
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        ForgotPasswordResponse response = await authService.ForgotPasswordAsync(request, HttpContext.RequestAborted);
        return Ok(response);
    }

    [AllowAnonymous]
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
}

```

---

## File: `eNote\eNote.Application\Common\Exceptions\AuthenticationException.cs`
**Hash**: `8d5c996fcfc4` | **Size**: 164 chars

**Classes**: AuthenticationException
```cs
namespace eNote.Application.Common.Exceptions;

public class AuthenticationException(string? message = null) : AppException(401, "error.unauthorized", message)
{
}

```

---

## File: `eNote\eNote.Application\Common\Exceptions\AuthorizationException.cs`
**Hash**: `9033d52670b6` | **Size**: 160 chars

**Classes**: AuthorizationException
```cs
namespace eNote.Application.Common.Exceptions;

public class AuthorizationException(string? message = null) : AppException(403, "error.forbidden", message)
{
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Auth\AuthResponse.cs`
**Hash**: `6a73946b1630` | **Size**: 479 chars

**Classes**: AuthResponse
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

---

## File: `eNote\eNote.Application\Features\Identity\Auth\ForgotPasswordRequest.cs`
**Hash**: `af23ef376863` | **Size**: 233 chars

**Classes**: ForgotPasswordRequest
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

---

## File: `eNote\eNote.Application\Features\Identity\Auth\ForgotPasswordResponse.cs`
**Hash**: `ec7a871362bf` | **Size**: 150 chars

**Classes**: ForgotPasswordResponse
```cs
namespace eNote.Application.Features.Identity.Auth;

public sealed class ForgotPasswordResponse
{
    public string Message { get; init; } = null!;
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Auth\LoginRequest.cs`
**Hash**: `f7e19892bb48` | **Size**: 345 chars

**Classes**: LoginRequest
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

---

## File: `eNote\eNote.Application\Features\Identity\Auth\RegisterRequest.cs`
**Hash**: `657a91aac283` | **Size**: 450 chars

**Classes**: RegisterRequest
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

---

## File: `eNote\eNote.Application\Features\Identity\Auth\ResetPasswordRequest.cs`
**Hash**: `921078e38309` | **Size**: 378 chars

**Classes**: ResetPasswordRequest
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

---

## File: `eNote\eNote.Application\Features\Identity\Auth\Services\IAuthService.cs`
**Hash**: `5ba4535f3d96` | **Size**: 697 chars

**Classes**: 
**Interfaces**: IAuthService
```cs
using eNote.Application.Features.Identity.Auth;

namespace eNote.Application.Features.Identity.Auth.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest model, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest model, CancellationToken cancellationToken = default);
    Task LogoutAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Auth\Services\ITokenRevocationService.cs`
**Hash**: `73e73c3ca240` | **Size**: 298 chars

**Classes**: 
**Interfaces**: ITokenRevocationService
```cs
namespace eNote.Application.Features.Identity.Auth.Services;

public interface ITokenRevocationService
{
    Task RevokeAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default);
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Auth\Services\ITokenService.cs`
**Hash**: `ee4525554437` | **Size**: 173 chars

**Classes**: 
**Interfaces**: ITokenService
```cs
namespace eNote.Application.Features.Identity.Auth.Services;

public interface ITokenService
{
    string GenerateToken(int userId, string username, IList<string> roles);
}

```

---

## File: `eNote\eNote.Infrastructure\Identity\AuthService.cs`
**Hash**: `777411f9afe5` | **Size**: 4854 chars

**Classes**: AuthService
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
    public async Task<AuthResponse> LoginAsync(LoginRequest model, CancellationToken cancellationToken = default)
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

    public async Task<AuthResponse> RegisterAsync(RegisterRequest model, CancellationToken cancellationToken = default)
    {
        (_, var error) = await userProvisioning.RegisterStudentAsync(model, cancellationToken);

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

---

