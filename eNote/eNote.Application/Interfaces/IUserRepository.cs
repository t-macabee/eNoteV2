using eNote.Application.Models.Auth;

namespace eNote.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<UserProfileResponse?> GetUserProfileAsync(int userId);
    }
}
