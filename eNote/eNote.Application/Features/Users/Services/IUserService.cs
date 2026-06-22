using eNote.Application.Features.Auth;

namespace eNote.Application.Features.Users.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileResponse?> GetCurrentUserAsync();
        Task<UserProfileResponse?> GetUserAsync(int userId);
        Task<(UserProfileResponse? Profile, string? Error)> RegisterStudentAsync(RegisterRequest request);
        Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request);
        Task<(bool Success, string? Error)> UpdateProfileAsync(UpdateProfileRequest request);
        Task<(bool Success, string? Error)> UpdatePictureAsync(byte[] picture);
        Task<(byte[]? Data, string? ContentType)> GetPictureAsync();
        Task<(bool Success, string? Error)> DeletePictureAsync();
        Task<(bool Success, string? Error)> ChangePasswordAsync(ChangePasswordRequest request);
        Task UpdateMembershipAsync(int userId, UpdateMembershipRequest request);
    }
}
