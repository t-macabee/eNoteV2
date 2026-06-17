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
    }
}
