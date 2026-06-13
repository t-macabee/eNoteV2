using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Auth.Services.Interfaces;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Identity
{
    public class TokenRevocationService(IAppDbContext context, IClock clock) : ITokenRevocationService
    {
        public async Task RevokeAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jti))
                return;

            var exists = await context.Set<RevokedToken>().AnyAsync(x => x.Jti == jti, cancellationToken);

            if (exists)
                return;

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
                return false;

            return await context.Set<RevokedToken>()
                .AsNoTracking()
                .AnyAsync(x => x.Jti == jti && x.ExpiresAt > clock.UtcNow, cancellationToken);
        }
    }
}
