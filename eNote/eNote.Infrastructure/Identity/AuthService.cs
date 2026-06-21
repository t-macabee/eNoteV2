using eNote.Application.Common.Localization;
using eNote.Application.Features.Auth;
using eNote.Application.Features.Auth.Services;
using eNote.Application.Features.Users.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace eNote.Infrastructure.Identity
{
    public class AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService, IUserService userService, ITokenRevocationService tokenRevocationService) : IAuthService
    {
        public async Task<(AuthResponse? response, string? error)> Login(LoginRequest model)
        {
            string username = model.Username.Trim();
            AppUser? user = await userManager.FindByNameAsync(username);

            if (user == null || !user.IsActive)
            {
                return (null, Messages.InvalidCredentials);
            }

            SignInResult result = await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                return (null, Messages.InvalidCredentials);
            }

            IList<string> roles = await userManager.GetRolesAsync(user);

            if (roles.Count != 1)
            {
                return (null, Messages.RoleMisconfigured);
            }

            string token = tokenService.GenerateToken(user.Id, user.UserName!, roles);

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
            (Application.Features.Users.UserProfileResponse _, string? error) = await userService.RegisterStudentAsync(model);

            if (error is not null)
            {
                return (null, error);
            }

            AppUser? user = await userManager.FindByNameAsync(model.Username.Trim());

            if (user is null)
            {
                return (null, Messages.InternalError);
            }

            IList<string> roles = await userManager.GetRolesAsync(user);

            string token = tokenService.GenerateToken(user.Id, user.UserName!, roles);

            return (new AuthResponse
            {
                UserId = user.Id,
                Username = user.UserName!,
                Roles = roles.ToList().AsReadOnly(),
                Token = token
            }, null);
        }

        public Task LogoutAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default) => tokenRevocationService.RevokeAsync(jti, expiresAtUtc, cancellationToken);

        public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, bool includeDevToken, CancellationToken cancellationToken = default)
        {
            string email = request.Email.Trim();
            AppUser? user = await userManager.FindByEmailAsync(email);

            if (user is null || !user.IsActive)
            {
                return new ForgotPasswordResponse { Message = Messages.PasswordResetEmailSent };
            }

            string token = await userManager.GeneratePasswordResetTokenAsync(user);

            return new ForgotPasswordResponse
            {
                Message = Messages.PasswordResetEmailSent,
                ResetToken = includeDevToken ? token : null
            };
        }

        public async Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            AppUser? user = await userManager.FindByEmailAsync(request.Email.Trim());

            if (user is null || !user.IsActive)
            {
                return (false, Messages.PasswordResetFailed);
            }

            IdentityResult result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (!result.Succeeded)
            {
                string errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return (false, Messages.PasswordResetFailed + " " + errors);
            }

            return (true, null);
        }
    }
}
