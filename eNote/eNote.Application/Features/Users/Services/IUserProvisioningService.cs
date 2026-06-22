using eNote.Application.Features.Auth;

namespace eNote.Application.Features.Users.Services;

public interface IUserProvisioningService
{
    Task<(UserProfileResponse? Profile, string? Error)> RegisterStudentAsync(RegisterRequest request);
    Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request);
    Task UpdateMembershipAsync(int userId, UpdateMembershipRequest request);
}
