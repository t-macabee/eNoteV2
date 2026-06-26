using eNote.Application.Features.Identity.Users;

namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserIdentityService
{
    Task<UserIdentityDto?> GetUserAsync(int userId);
    Task<IReadOnlyDictionary<int, UserIdentityDto>> GetUsersBulkAsync(IEnumerable<int> userIds);
    Task<IReadOnlyList<string>> GetRolesAsync(int userId);
}
