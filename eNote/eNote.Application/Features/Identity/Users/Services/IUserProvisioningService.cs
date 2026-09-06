using eNote.Application.Features.Identity.Auth;

namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserProvisioningService
{
    Task<(RegistrationResult? Registration, string? Error)> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default);
    Task UpdateMembershipAsync(int userId, UpdateMembershipRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeactivateUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> SetUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> IsStoreManagerAsync(int userId, CancellationToken cancellationToken = default);
    Task<(int UserId, string? Error)> ProvisionStudentByInstructorAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default);
    Task<(int UserId, string? Error)> ProvisionEmployeeByManagerAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default);
}
