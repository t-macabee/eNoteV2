namespace eNote.Application.Features.Auth.Services
{
    public interface IAuthService
    {
        Task<(AuthResponse? response, string? error)> Login(LoginRequest model);
        Task<(AuthResponse? response, string? error)> Register(RegisterRequest model);
        Task LogoutAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default);
    }
}
