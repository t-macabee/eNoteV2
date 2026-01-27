using eNote.Application.Features.Users.DTOs;

namespace eNote.Application.Features.Users.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileResponse?> GetCurrentUserAsync(int userId);
    }
}
