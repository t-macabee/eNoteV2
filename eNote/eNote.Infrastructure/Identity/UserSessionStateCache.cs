using eNote.Application.Common.Persistence;
using eNote.Application.Features.Identity.Auth.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace eNote.Infrastructure.Identity;

public sealed class UserSessionStateCache(IAppDbContext context, IMemoryCache cache) : IUserSessionStateCache
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    private static string Key(int userId) => $"user-session:{userId}";

    public async Task<UserSessionState?> GetAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(Key(userId), out UserSessionState? cached))
        {
            return cached;
        }

        var state = await context.Set<AppUser>()
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new UserSessionState(x.IsActive, x.SecurityStamp))
            .FirstOrDefaultAsync(cancellationToken);

        if (state is not null)
        {
            cache.Set(Key(userId), state, CacheDuration);
        }

        return state;
    }

    public void Invalidate(int userId) => cache.Remove(Key(userId));
}
