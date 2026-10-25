namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserAccountService
{
    Task<int?> FindUserIdByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<int> CreateUserAsync(string username, string email, string password, string? firstName, string? lastName, CancellationToken cancellationToken = default);
    Task AssignSingleRoleAsync(int userId, string role, CancellationToken cancellationToken = default);
    Task UpdateExistingUserAsync(int userId, string email, string? firstName, string? lastName, DateTime? dateOfBirth = null, CancellationToken cancellationToken = default);
    Task UpdatePictureAsync(int userId, Stream picture, string contentType, CancellationToken cancellationToken = default);
    Task<(Stream? Data, string? ContentType)> GetPictureAsync(int userId, CancellationToken cancellationToken = default);
    Task DeletePictureAsync(int userId, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<bool> VerifyPasswordAsync(int userId, string currentPassword, CancellationToken cancellationToken = default);
    Task SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default);
    Task<string?> CloseAccountAsync(int userId, CancellationToken cancellationToken = default);
    Task<string?> DeleteUserAsync(int userId, CancellationToken cancellationToken = default);
}
