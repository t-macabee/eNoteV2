using eNote.Application.Common.Localization;
using eNote.Application.Features.Auth;
using eNote.Application.Features.Auth.Services;
using eNote.Application.Features.Users.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eNote.Infrastructure.Identity;

public class AuthService(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ITokenService tokenService,
    IUserService userService,
    ITokenRevocationService tokenRevocationService,
    IWebHostEnvironment environment,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<(AuthResponse? response, string? error)> Login(LoginRequest model)
    {
        var username = model.Username.Trim();
        var user = await userManager.FindByNameAsync(username);

        if (user == null || !user.IsActive)
        {
            return (null, Messages.InvalidCredentials);
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return (null, Messages.AccountLocked);
        }

        if (!result.Succeeded)
        {
            return (null, Messages.InvalidCredentials);
        }

        var roles = await userManager.GetRolesAsync(user);

        if (roles.Count != 1)
        {
            return (null, Messages.RoleMisconfigured);
        }

        var token = tokenService.GenerateToken(user.Id, user.UserName!, roles);

        return (new AuthResponse
        {
            UserId = user.Id,
            Username = user.UserName!,
            Roles = roles.ToList().AsReadOnly(),
            Token = token
        }, null);
    }

    public async Task<(AuthResponse? response, string? error)> Register(RegisterRequest model)
    {
        (_, var error) = await userService.RegisterStudentAsync(model);

        if (error is not null)
        {
            return (null, error);
        }

        var user = await userManager.FindByNameAsync(model.Username.Trim());

        if (user is null)
        {
            return (null, Messages.InternalError);
        }

        var roles = await userManager.GetRolesAsync(user);

        var token = tokenService.GenerateToken(user.Id, user.UserName!, roles);

        return (new AuthResponse
        {
            UserId = user.Id,
            Username = user.UserName!,
            Roles = roles.ToList().AsReadOnly(),
            Token = token
        }, null);
    }

    public Task LogoutAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default) =>
        tokenRevocationService.RevokeAsync(jti, expiresAtUtc, cancellationToken);

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null || !user.IsActive)
        {
            return new ForgotPasswordResponse { Message = Messages.PasswordResetEmailSent };
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        if (environment.IsDevelopment())
        {
            logger.LogInformation("Password reset token for {Email}: {Token}", email, token);
        }

        return new ForgotPasswordResponse { Message = Messages.PasswordResetEmailSent };
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null || !user.IsActive)
        {
            return (false, Messages.PasswordResetFailed);
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return (false, Messages.PasswordResetFailed + " " + errors);
        }

        return (true, null);
    }
}