using eNote.Application.DTOs.Auth;

namespace eNote.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileResponse?> GetCurrentUserAsync(int userId);
    }
}
