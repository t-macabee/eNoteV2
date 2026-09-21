namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserIdentityService
{
    Task<UserIdentityDto?> GetUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, UserIdentityDto>> GetUsersBulkAsync(IEnumerable<int> userIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRolesAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> FindUserIdsAsync(string? name, bool? isActive, CancellationToken cancellationToken = default);
}
