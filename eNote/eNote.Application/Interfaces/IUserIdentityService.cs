using eNote.Application.DTOs.Users;

namespace eNote.Application.Interfaces
{
    public interface IUserIdentityService
    {
        Task<UserIdentityDto?> GetUserAsync(int userId);
        Task<IReadOnlyList<string>> GetRolesAsync(int userId);
    }
}
