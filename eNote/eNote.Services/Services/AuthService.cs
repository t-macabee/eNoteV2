using eNote.Application.Interfaces;
using eNote.Infrastructure.Identity;
using eNote.Model.Auth;
using eNote.Service.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace eNote.Service.Services
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

            if (user == null || !user.Status)
                return (null, "Pogrešno korisničko ime ili lozinka.");

            if(!(await _signInManager.CheckPasswordSignInAsync(user, model.Password, false)).Succeeded)
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