namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserAccountService
{
    Task<int?> FindUserIdByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<(int? UserId, string? Error)> CreateUserAsync(string username, string email, string password, string? firstName, string? lastName, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> AssignSingleRoleAsync(int userId, string role, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateExistingUserAsync(int userId, string email, string? firstName, string? lastName, DateTime? dateOfBirth = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdatePictureAsync(int userId, Stream picture, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<(Stream? Data, string? ContentType)> GetPictureAsync(int userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeletePictureAsync(int userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteUserAsync(int userId, CancellationToken cancellationToken = default);
}
