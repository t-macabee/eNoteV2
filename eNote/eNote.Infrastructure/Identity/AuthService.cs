using eNote.Application.Features.Auth.DTOs;
using eNote.Application.Features.Auth.Requests;
using eNote.Application.Features.Auth.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace eNote.Infrastructure.Identity
{
    public class AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService) : IAuthService
    {
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly SignInManager<AppUser> _signInManager = signInManager;
        private readonly ITokenService _tokenService = tokenService;

        public async Task<(AuthResponse? response, string? error)> Login(LoginRequest model)
        {
            var username = model.Username.Trim();

            var user = await _userManager.FindByNameAsync(username);
            if (user == null || !user.IsActive)
                return (null, "Pogrešno korisničko ime ili lozinka.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);

            if (!result.Succeeded)
                return (null, "Pogrešno korisničko ime ili lozinka.");

            var roles = await _userManager.GetRolesAsync(user);

            var token = _tokenService.GenerateToken(user.Id, user.UserName!, roles);

            return (new AuthResponse
            {
                UserId = user.Id,
                Username = user.UserName!,
                Roles = roles.ToList().AsReadOnly(),                
                Token = token
            }, null);
        }
    }
}