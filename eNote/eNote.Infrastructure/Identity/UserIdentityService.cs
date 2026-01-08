using eNote.Application.DTOs;
using eNote.Application.Interfaces.Identity;
using eNote.Application.Interfaces.Ports;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Identity
{
    public class UserIdentityService(UserManager<AppUser> userManager, IAppDbContext context) : IUserIdentityService
    {
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly IAppDbContext _context = context;

        public async Task<UserIdentityDto?> GetUserAsync(int userId)
        {
            var user = await _userManager.Users.AsNoTracking().Include(u => u.Address).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return null;

            return new UserIdentityDto
            {
                Id = user.Id,
                Username = user.UserName!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address == null
                ? null 
                : new AddressDto
                {
                    City = user.Address.City,
                    Street = user.Address.Street,
                    Number = user.Address.Number
                },
                IsActive = user.IsActive
            };
        }

        public async Task<IReadOnlyList<string>> GetRolesAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return [];

            return [.. await _userManager.GetRolesAsync(user)];
        }
    }
}
