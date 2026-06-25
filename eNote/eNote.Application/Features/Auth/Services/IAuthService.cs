namespace eNote.Application.Features.Auth.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest model);
    Task<AuthResponse> RegisterAsync(RegisterRequest model);
    Task LogoutAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
