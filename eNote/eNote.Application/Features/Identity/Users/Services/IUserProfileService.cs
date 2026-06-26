using eNote.Application.Features.Identity.Users;

namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserProfileService
{
    Task<UserProfileResponse?> GetCurrentUserAsync();
    Task<UserProfileResponse?> GetUserAsync(int userId);
}
