using eNote.Application.Features.Users.DTOs;

namespace eNote.Application.Features.Users.Services.Interfaces
{
    public interface IUserIdentityService
    {
        Task<UserIdentityDto?> GetUserAsync(int userId);
        Task<IReadOnlyList<string>> GetRolesAsync(int userId);
    }
}
