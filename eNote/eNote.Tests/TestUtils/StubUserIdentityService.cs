using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Tests.TestUtils;

public sealed class StubUserIdentityService : IUserIdentityService
{
    private readonly Dictionary<int, UserIdentityDto> _users;
    private readonly Dictionary<int, IReadOnlyList<string>> _roles;

    public StubUserIdentityService(Dictionary<int, UserIdentityDto>? users = null, Dictionary<int, IReadOnlyList<string>>? roles = null)
    {
        _users = users ?? [];
        _roles = roles ?? [];
    }

    public Task<UserIdentityDto?> GetUserAsync(int userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.GetValueOrDefault(userId));

    public Task<IReadOnlyDictionary<int, UserIdentityDto>> GetUsersBulkAsync(IEnumerable<int> userIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<int, UserIdentityDto>>(
            userIds.Where(_users.ContainsKey).ToDictionary(id => id, id => _users[id]));

    public Task<IReadOnlyList<string>> GetRolesAsync(int userId) =>
        Task.FromResult(_roles.GetValueOrDefault(userId) ?? []);

    public static UserIdentityDto User(int id, string username, string? firstName = null, string? lastName = null) => new()
    {
        Id = id,
        Username = username,
        FirstName = firstName,
        LastName = lastName,
        IsActive = true
    };
}
