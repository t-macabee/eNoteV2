using eNote.Application.Features.Identity.Auth;

namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserProvisioningService
{
    Task<RegistrationResult> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<int> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default);
    Task SetUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> IsStoreManagerAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> ProvisionStudentByInstructorAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default);
    Task<int> ProvisionEmployeeByManagerAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default);
}
