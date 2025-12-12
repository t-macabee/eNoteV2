using eNote.Contracts.DTOs.Auth;

namespace eNote.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileResult?> GetCurrentUserAsync(int userId);
    }
}
