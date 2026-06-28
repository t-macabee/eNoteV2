using eNote.Application.Features.Identity.Users;

namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserProfileService
{
    Task<UserProfileResponse?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<UserProfileResponse?> GetUserAsync(int userId, CancellationToken cancellationToken = default);
}
