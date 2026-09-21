using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Identity;

namespace eNote.Application.Features.Identity.Users;

public static class UserProfileQueryExtensions
{
    public static async Task<IQueryable<TEntity>> WhereUserMatchesAsync<TEntity>(
        this IQueryable<TEntity> query,
        IUserIdentityService identity,
        string? name,
        bool? isActive,
        CancellationToken ct = default) where TEntity : IHasAppUserId
    {
        if (string.IsNullOrWhiteSpace(name) && !isActive.HasValue)
        {
            return query;
        }

        var ids = await identity.FindUserIdsAsync(name, isActive, ct);
        return query.Where(x => ids.Contains(x.AppUserId));
    }
}
