using eNote.Application.Common.Localization;
using eNote.Application.Features.Auth;
using eNote.Application.Features.Auth.Services;
using eNote.Application.Features.Users.Services;
using Microsoft.AspNetCore.Identity;

namespace eNote.Infrastructure.Identity
{
    public class AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService, IUserService userService, ITokenRevocationService tokenRevocationService) : IAuthService
    {
        public async Task<(AuthResponse? response, string? error)> Login(LoginRequest model)
        {
            var username = model.Username.Trim();

            var user = await userManager.FindByNameAsync(username);

            if (user == null || !user.IsActive)
                return (null, Messages.InvalidCredentials);

            var result = await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);

            if (!result.Succeeded)
                return (null, Messages.InvalidCredentials);

            var roles = await userManager.GetRolesAsync(user);

            if (roles.Count != 1)
                return (null, Messages.RoleMisconfigured);

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
            var (_, error) = await userService.RegisterStudentAsync(model);

            if (error is not null)
                return (null, error);

            var user = await userManager.FindByNameAsync(model.Username.Trim());

            if (user is null)
                return (null, Messages.InternalError);

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

        public Task LogoutAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default) => tokenRevocationService.RevokeAsync(jti, expiresAtUtc, cancellationToken);
    }
}
