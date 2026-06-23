using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Features.Auth;
using eNote.Application.Features.Auth.Services;
using eNote.Application.Features.Users.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eNote.Infrastructure.Identity;

public class AuthService(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ITokenService tokenService,
    IUserProvisioningService userProvisioning,
    ITokenRevocationService tokenRevocationService,
    IWebHostEnvironment environment,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest model)
    {
        string username = model.Username.Trim();
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

        string token = tokenService.GenerateToken(user.Id, user.UserName!, roles);

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

        string token = tokenService.GenerateToken(user.Id, user.UserName!, roles);

        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.UserName!,
            Roles = roles.ToList().AsReadOnly(),
            Token = token
        };
    }

    public Task LogoutAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default) =>
        tokenRevocationService.RevokeAsync(jti, expiresAtUtc, cancellationToken);

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        string email = request.Email.Trim();
        AppUser? user = await userManager.FindByEmailAsync(email);

        if (user is null || !user.IsActive)
        {
            return new ForgotPasswordResponse { Message = Messages.PasswordResetEmailSent };
        }

        string token = await userManager.GeneratePasswordResetTokenAsync(user);

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
            string errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new BusinessException(Messages.PasswordResetFailed + " " + errors);
        }
    }
}
