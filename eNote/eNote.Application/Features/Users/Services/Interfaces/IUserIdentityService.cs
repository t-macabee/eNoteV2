namespace eNote.Application.Features.Users.Services.Interfaces
{
    public interface IUserIdentityService
    {
        Task<UserIdentityDto?> GetUserAsync(int userId);
        Task<IReadOnlyDictionary<int, UserIdentityDto>> GetUsersBulkAsync(IEnumerable<int> userIds);
        Task<IReadOnlyList<string>> GetRolesAsync(int userId);
    }
}
