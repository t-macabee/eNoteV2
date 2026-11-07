using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace eNote.Tests.Identity;

public sealed class TokenRevocationServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    private const int RevokingUserId = 7;

    [Fact]
    public async Task RevokeAsync_PersistsRevokedToken()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context);

        await service.RevokeAsync("jti-1", Now.AddHours(1), RevokingUserId);

        var row = await context.Set<RevokedToken>().SingleAsync();
        Assert.Equal(RevokingUserId, row.UserId);
        Assert.Equal("jti-1", row.Jti);
        Assert.Equal(Now.AddHours(1), row.ExpiresAt);
        Assert.Equal(Now, row.RevokedAt);
    }

    [Fact]
    public async Task IsRevokedAsync_ReturnsTrue_AfterRevoke()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context);

        await service.RevokeAsync("jti-1", Now.AddHours(1), RevokingUserId);

        Assert.True(await service.IsRevokedAsync("jti-1"));
    }

    [Fact]
    public async Task IsRevokedAsync_ReturnsFalse_WhenTokenExpired()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context);

        await service.RevokeAsync("jti-1", Now.AddHours(-1), RevokingUserId);

        Assert.False(await service.IsRevokedAsync("jti-1"));
    }

    [Fact]
    public async Task IsRevokedAsync_ReturnsFalse_ForUnknownAndBlankJti()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context);

        Assert.False(await service.IsRevokedAsync("unknown"));
        Assert.False(await service.IsRevokedAsync("  "));
    }

    [Fact]
    public async Task IsRevokedAsync_ReturnsFalse_ForOtherJti()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context);

        await service.RevokeAsync("jti-1", Now.AddHours(1), RevokingUserId);

        Assert.False(await service.IsRevokedAsync("jti-2"));
    }

    [Fact]
    public async Task IsRevokedAsync_CachesTheNegativeResult()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context);

        Assert.False(await service.IsRevokedAsync("j1"));

        context.Set<RevokedToken>().Add(new RevokedToken
        {
            Jti = "j1",
            UserId = RevokingUserId,
            ExpiresAt = Now.AddHours(1),
            RevokedAt = Now
        });
        await context.SaveChangesAsync();

        Assert.False(await service.IsRevokedAsync("j1"));
    }

    [Fact]
    public async Task RevokeAsync_CachesTrue_UntilTheTokenExpires()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context);

        await service.RevokeAsync("j2", Now.AddHours(1), RevokingUserId);

        var row = await context.Set<RevokedToken>().SingleAsync(x => x.Jti == "j2");
        context.Set<RevokedToken>().Remove(row);
        await context.SaveChangesAsync();

        Assert.True(await service.IsRevokedAsync("j2"));
    }

    [Fact]
    public async Task RevokeAsync_IgnoresBlankJti()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context);

        await service.RevokeAsync("  ", Now.AddHours(1), RevokingUserId);

        Assert.Equal(0, await context.Set<RevokedToken>().CountAsync());
    }

    [Fact]
    public async Task RevokeAsync_DoesNotDuplicate_WhenAlreadyRevoked()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context);

        await service.RevokeAsync("jti-1", Now.AddHours(1), RevokingUserId);
        await service.RevokeAsync("jti-1", Now.AddHours(1), RevokingUserId);

        Assert.Equal(1, await context.Set<RevokedToken>().CountAsync());
    }

    [Fact]
    public async Task RevokeAsync_TreatsDuplicateJtiViolation_AsSuccess()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.RevokedTokenJtiUniqueIndex}\"");
        var service = CreateService(new ThrowingSaveDbContext(context, new DbUpdateException("Unique constraint violated.", inner)));

        await service.RevokeAsync("jti-1", Now.AddHours(1), RevokingUserId);

        Assert.True(await service.IsRevokedAsync("jti-1"));
    }

    [Fact]
    public async Task RevokeAsync_PropagatesUnrelatedDbUpdateException()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var inner = new Exception("some other constraint");
        var service = CreateService(new ThrowingSaveDbContext(context, new DbUpdateException("Unique constraint violated.", inner)));

        await Assert.ThrowsAsync<DbUpdateException>(() => service.RevokeAsync("jti-1", Now.AddHours(1), RevokingUserId));
    }

    private static TokenRevocationService CreateService(IAppDbContext context) =>
        new(context, new FixedClock(Now), new MemoryCache(new MemoryCacheOptions()));
}
