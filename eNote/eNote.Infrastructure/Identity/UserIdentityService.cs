using eNote.Application.DTOs.Users;
using eNote.Application.Interfaces;
using eNote.Application.Models.Shared;
using eNote.Infrastructure.Data.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Identity
{
    public class UserIdentityService(UserManager<AppUser> userManager, ENoteContext context) : IUserIdentityService
    {
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly ENoteContext _context = context;

        public async Task<UserIdentityDto?> GetUserAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking().Include(u => u.Address).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return null;

            return new UserIdentityDto(user.Id, user.UserName!, user.FirstName, user.LastName, user.DateOfBirth, user.Address == null 
                ? null : new AddressDto(user.Address.City, user.Address.Street, user.Address.Number), user.IsActive);
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
