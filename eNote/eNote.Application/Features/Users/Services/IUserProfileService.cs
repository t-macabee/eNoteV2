namespace eNote.Application.Features.Users.Services;

public interface IUserProfileService
{
    Task<UserProfileResponse?> GetCurrentUserAsync();
    Task<UserProfileResponse?> GetUserAsync(int userId);
}
