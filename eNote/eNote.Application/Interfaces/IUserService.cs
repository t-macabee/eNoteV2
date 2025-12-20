using eNote.Application.DTOs.Profiles;

namespace eNote.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileResponse?> GetCurrentUserAsync(int userId);
    }
}
