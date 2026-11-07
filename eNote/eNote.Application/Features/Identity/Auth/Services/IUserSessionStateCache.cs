namespace eNote.Application.Features.Identity.Auth.Services;

public interface IUserSessionStateCache
{
    Task<UserSessionState?> GetAsync(int userId, CancellationToken cancellationToken = default);
    void Invalidate(int userId);
}
