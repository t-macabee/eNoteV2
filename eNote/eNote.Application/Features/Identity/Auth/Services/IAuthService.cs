namespace eNote.Application.Features.Identity.Auth.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest model, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest model, CancellationToken cancellationToken = default);
    Task LogoutAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
