using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Auth.Services;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eNote.Infrastructure.Identity;

internal sealed class AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService, IUserProvisioningService userProvisioning, ITokenRevocationService tokenRevocationService, IEmailService emailService, IHostEnvironment environment, ILogger<AuthService> logger) : IAuthService
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
        (var registration, var error) = await userProvisioning.RegisterStudentAsync(model, cancellationToken);

        if (error is not null)
        {
            if (error == Messages.UsernameTaken || error == Messages.EmailTaken)
            {
                throw new ConflictException(Messages.UsernameTaken);
            }

            throw new BusinessException(error);
        }

        var registeredUser = registration ?? throw new BusinessException(Messages.InternalError);
        var token = tokenService.GenerateToken(registeredUser.UserId, registeredUser.Username, registeredUser.Roles.ToList());

        return new AuthResponse
        {
            UserId = registeredUser.UserId,
            Username = registeredUser.Username,
            Roles = registeredUser.Roles,
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
            logger.LogInformation("Password reset token generated for {Email}: {Token}...", email, token[..Math.Min(8, token.Length)]);
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

            logger.LogWarning("Password reset failed for user {UserId}: {Errors}", user.Id, errors);

            throw new BusinessException(Messages.PasswordResetFailed);
        }
    }
}
