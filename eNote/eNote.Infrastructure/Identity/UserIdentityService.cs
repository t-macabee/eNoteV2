using eNote.Application.Common.Paging;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Identity;

public sealed class UserIdentityService(UserManager<AppUser> userManager, ENoteContext context) : IUserIdentityService
{
    public async Task<UserIdentityDto?> GetUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user is null ? null : Map(user);
    }

    public async Task<IReadOnlyDictionary<int, UserIdentityDto>> GetUsersBulkAsync(IEnumerable<int> userIds, CancellationToken cancellationToken = default)
    {
        HashSet<int> ids = [.. userIds];

        if (ids.Count == 0)
        {
            return new Dictionary<int, UserIdentityDto>();
        }

        var users = await userManager.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(cancellationToken);

        return users.ToDictionary(u => u.Id, Map);
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(int userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());

        return user is null ? [] : [.. await userManager.GetRolesAsync(user)];
    }

    public async Task<IReadOnlyList<int>> FindUserIdsAsync(string? name, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = name.Trim().ToLower();
            query = query.Where(u =>
                (u.FirstName != null && u.FirstName.ToLower().Contains(term)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(term)) ||
                (u.UserName != null && u.UserName.ToLower().Contains(term)) ||
                (((u.FirstName ?? "") + " " + (u.LastName ?? "")).ToLower().Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        return await query.Select(u => u.Id).ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<AdminUserListItemDto>> SearchAsync(AdminUserSearchObject search, CancellationToken cancellationToken = default)
    {
        var query = userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            var term = search.Name.Trim().ToLower();
            query = query.Where(u =>
                (u.FirstName != null && u.FirstName.ToLower().Contains(term)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(term)) ||
                (u.UserName != null && u.UserName.ToLower().Contains(term)) ||
                (((u.FirstName ?? "") + " " + (u.LastName ?? "")).ToLower().Contains(term)));
        }

        if (search.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == search.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.Role))
        {
            var role = search.Role;
            query = query.Where(u => context.UserRoles.Any(ur =>
                ur.UserId == u.Id && context.Roles.Any(r => r.Id == ur.RoleId && r.Name == role)));
        }

        return await query.ToPagedResultAsync(
            search,
            MapPageAsync,
            q => q.OrderBy(u => context.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .FirstOrDefault())
                .ThenBy(u => u.LastName)
                .ThenBy(u => u.FirstName),
            cancellationToken);
    }

    private async Task<IEnumerable<AdminUserListItemDto>> MapPageAsync(List<AppUser> users, CancellationToken cancellationToken)
    {
        var ids = users.Select(u => u.Id).ToList();

        var roles = await context.UserRoles
            .AsNoTracking()
            .Where(ur => ids.Contains(ur.UserId))
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToDictionaryAsync(x => x.UserId, x => x.Name, cancellationToken);

        var students = await context.Set<Student>()
            .AsNoTracking()
            .Where(s => ids.Contains(s.AppUserId))
            .ToDictionaryAsync(s => s.AppUserId, cancellationToken);

        var employees = await context.Set<MusicStoreEmployee>()
            .AsNoTracking()
            .Where(e => ids.Contains(e.AppUserId) && e.IsActive)
            .Select(e => new { e.AppUserId, e.IsManager, e.MusicStore.StoreName })
            .ToDictionaryAsync(e => e.AppUserId, cancellationToken);

        return users.Select(u =>
        {
            students.TryGetValue(u.Id, out var student);
            employees.TryGetValue(u.Id, out var employee);

            return new AdminUserListItemDto
            {
                AppUserId = u.Id,
                Username = u.UserName!,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = roles.GetValueOrDefault(u.Id),
                IsActive = u.IsActive,
                StudentId = student?.Id,
                MembershipPaidUntil = student?.MembershipPaidUntil,
                EnrollmentDate = student?.EnrollmentDate,
                StoreName = employee?.StoreName,
                IsManager = employee?.IsManager
            };
        }).ToList();
    }

    private static UserIdentityDto Map(AppUser user) => new()
    {
        Id = user.Id,
        Username = user.UserName!,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        DateOfBirth = user.DateOfBirth,
        HasPicture = !string.IsNullOrWhiteSpace(user.PicturePath),
        IsActive = user.IsActive
    };
}
