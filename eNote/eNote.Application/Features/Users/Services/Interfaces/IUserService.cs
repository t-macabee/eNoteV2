using eNote.Application.Features.Auth;

namespace eNote.Application.Features.Users.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileResponse?> GetCurrentUserAsync();
        Task<UserProfileResponse?> GetUserAsync(int userId);
        Task<(UserProfileResponse? Profile, string? Error)> RegisterStudentAsync(RegisterRequest request);
        Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request);
    }
}
