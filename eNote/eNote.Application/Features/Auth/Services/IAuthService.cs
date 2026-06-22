namespace eNote.Application.Features.Auth.Services
{
    public interface IAuthService
    {
        Task<(AuthResponse? response, string? error)> Login(LoginRequest model);
        Task<(AuthResponse? response, string? error)> Register(RegisterRequest model);
        Task LogoutAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default);
        Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
        Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    }
}
