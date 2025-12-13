using eNote.Model.Auth;

namespace eNote.Service.Interfaces
{
    public interface IAuthService
    {
        Task<(AuthResponse? response, string? error)> Login(LoginRequest model);
    }
}
