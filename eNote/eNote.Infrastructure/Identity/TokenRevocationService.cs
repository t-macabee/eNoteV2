using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Auth.Services;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace eNote.Infrastructure.Identity;

public class TokenRevocationService(IAppDbContext context, IClock clock, IMemoryCache cache) : ITokenRevocationService
{
    private static string Key(string jti) => $"revoked:{jti}";

    public async Task RevokeAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return;
        }

        var ttl = expiresAt - clock.UtcNow;

        if (ttl > TimeSpan.Zero)
        {
            cache.Set(Key(jti), true, ttl);
        }

        var exists = await context.Set<RevokedToken>()
            .AnyAsync(x => x.Jti == jti, cancellationToken);

        if (exists)
        {
            return;
        }

        context.Set<RevokedToken>().Add(new RevokedToken
        {
            Jti = jti,
            ExpiresAt = expiresAt,
            RevokedAt = clock.UtcNow
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return false;
        }

        if (cache.TryGetValue(Key(jti), out _))
        {
            return true;
        }

        var revoked = await context.Set<RevokedToken>()
            .AsNoTracking()
            .AnyAsync(x => x.Jti == jti && x.ExpiresAt > clock.UtcNow, cancellationToken);

        if (revoked)
        {
            cache.Set(Key(jti), true, TimeSpan.FromHours(1));
        }

        return revoked;
    }
}
