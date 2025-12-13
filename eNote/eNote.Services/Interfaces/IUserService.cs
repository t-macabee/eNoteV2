using eNote.Model.Auth;

namespace eNote.Service.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileResponse?> GetCurrentUserAsync(int userId);
    }
}
