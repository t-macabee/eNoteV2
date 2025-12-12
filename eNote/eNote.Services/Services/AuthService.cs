using eNote.Application.Interfaces;
using eNote.Contracts.DTOs.Auth;
using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace eNote.Application.Services
{
    public class AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService, IUserService userService) : IAuthService
    {
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly SignInManager<AppUser> _signInManager = signInManager;
        private readonly ITokenService _tokenService = tokenService;
        private readonly IUserService _userService = userService;

        public async Task<(AuthResponse? response, string? error)> Login(LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);

            if (user == null || !user.Status)
                return (null, "Pogrešno korisničko ime ili lozinka.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (!result.Succeeded)
                return (null, "Pogrešno korisničko ime ili lozinka.");

            var roles = await _userManager.GetRolesAsync(user);

            var token = _tokenService.GenerateToken(user, roles);

            return (new AuthResponse
            {
                UserId = user.Id,
                Username = user.UserName!,
                Roles = roles.ToList().AsReadOnly(),                    
                Status = user.Status,
                Token = token
            }, null);
        }        
    }
}