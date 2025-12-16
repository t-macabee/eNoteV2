using eNote.Application.Interfaces;
using eNote.Application.Models.Auth;

namespace eNote.Application.Services
{
    public class UserService(IUserRepository userRepository) : IUserService
    {
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<UserProfileResponse?> GetCurrentUserAsync(int userId)
        {
            return await _userRepository.GetUserProfileAsync(userId);
        }
    }    
}
