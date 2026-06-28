using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Users;

namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserProvisioningService
{
    Task<(UserProfileResponse? Profile, string? Error)> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default);
    Task UpdateMembershipAsync(int userId, UpdateMembershipRequest request, CancellationToken cancellationToken = default);
}
