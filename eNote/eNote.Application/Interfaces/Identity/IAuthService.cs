using eNote.Application.Models.Auth;

namespace eNote.Application.Interfaces.Identity
{
    public interface IAuthService
    {
        Task<(AuthResponse? response, string? error)> Login(LoginRequest model);
    }
}
