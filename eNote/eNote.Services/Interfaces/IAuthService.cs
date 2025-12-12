using eNote.Contracts.DTOs.Auth;

namespace eNote.Application.Interfaces
{
    public interface IAuthService
    {
        Task<(AuthResponse? response, string? error)> Login(LoginModel model);
    }
}
