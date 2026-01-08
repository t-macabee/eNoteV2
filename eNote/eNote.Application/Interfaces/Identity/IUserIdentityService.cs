using eNote.Application.DTOs;

namespace eNote.Application.Interfaces.Identity
{
    public interface IUserIdentityService
    {
        Task<UserIdentityDto?> GetUserAsync(int userId);
        Task<IReadOnlyList<string>> GetRolesAsync(int userId);
    }
}
