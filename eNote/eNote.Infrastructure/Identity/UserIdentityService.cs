using eNote.Application.Features.Users;
using eNote.Application.Features.Users.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Identity
{
    public class UserIdentityService(UserManager<AppUser> userManager) : IUserIdentityService
    {
        public async Task<UserIdentityDto?> GetUserAsync(int userId)
        {
            AppUser? user = await userManager.Users
                .AsNoTracking()
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

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

        public async Task<IReadOnlyDictionary<int, UserIdentityDto>> GetUsersBulkAsync(IEnumerable<int> userIds)
        {
            HashSet<int> ids = userIds.ToHashSet();

            if (ids.Count == 0)
            {
                return new Dictionary<int, UserIdentityDto>();
            }

            List<AppUser> users = await userManager.Users
                .AsNoTracking()
                .Include(u => u.Address)
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            return users.ToDictionary(u => u.Id, u => new UserIdentityDto
            {
                Id = u.Id,
                Username = u.UserName!,
                FirstName = u.FirstName,
                LastName = u.LastName,
                DateOfBirth = u.DateOfBirth,
                Address = u.Address == null ? null : new AddressDto
                {
                    City = u.Address.City,
                    Street = u.Address.Street,
                    Number = u.Address.Number
                },
                IsActive = u.IsActive
            });
        }

        public async Task<IReadOnlyList<string>> GetRolesAsync(int userId)
        {
            AppUser? user = await userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                return [];
            }

            return [.. await userManager.GetRolesAsync(user)];
        }
    }
}
