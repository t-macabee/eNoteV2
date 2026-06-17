namespace eNote.Application.Features.Users.Services
{
    public interface IUserAccountService
    {
        Task<int?> FindUserIdByUsernameAsync(string username);
        Task<(int? UserId, string? Error)> CreateUserAsync(string username, string email, string password, string? firstName, string? lastName);
        Task<(bool Success, string? Error)> AssignSingleRoleAsync(int userId, string role);
        Task<(bool Success, string? Error)> UpdateExistingUserAsync(int userId, string email, string? firstName, string? lastName);
        Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}
