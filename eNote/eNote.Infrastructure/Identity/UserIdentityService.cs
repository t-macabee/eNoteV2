using eNote.Application.Features.Users;
using eNote.Application.Features.Users.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Identity;

public sealed class UserIdentityService(UserManager<AppUser> userManager) : IUserIdentityService
{
    public async Task<UserIdentityDto?> GetUserAsync(int userId)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .Include(u => u.Address)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user is null ? null : Map(user);
    }

    public async Task<IReadOnlyDictionary<int, UserIdentityDto>> GetUsersBulkAsync(IEnumerable<int> userIds)
    {
        HashSet<int> ids = [.. userIds];

        if (ids.Count == 0)
        {
            return new Dictionary<int, UserIdentityDto>();
        }

        var users = await userManager.Users
            .AsNoTracking()
            .Include(u => u.Address)
            .Where(u => ids.Contains(u.Id))
            .ToListAsync();

        return users.ToDictionary(u => u.Id, Map);
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        return user is null ? [] : [.. await userManager.GetRolesAsync(user)];
    }

    private static UserIdentityDto Map(AppUser user) => new()
    {
        Id = user.Id,
        Username = user.UserName!,
        FirstName = user.FirstName,
        LastName = user.LastName,
        DateOfBirth = user.DateOfBirth,
        HasPicture = user.Picture is { Length: > 0 },
        Address = user.Address is null ? null : new AddressDto
        {
            City = user.Address.City,
            Street = user.Address.Street,
            Number = user.Address.Number
        },
        IsActive = user.IsActive
    };
}
