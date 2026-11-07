namespace eNote.Application.Features.Identity.Auth.Services;

public interface ITokenRevocationService
{
    Task RevokeAsync(string jti, DateTime expiresAt, int userId, CancellationToken cancellationToken = default);
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
}
