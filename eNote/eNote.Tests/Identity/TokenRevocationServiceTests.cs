using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace eNote.Tests.Identity;

public sealed class TokenRevocationServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RevokeAsync_PersistsRevokedToken()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context);

        await service.RevokeAsync("jti-1", Now.AddHours(1));

        var row = await context.Set<RevokedToken>().SingleAsync();
        Assert.Equal("jti-1", row.Jti);
        Assert.Equal(Now.AddHours(1), row.ExpiresAt);
        Assert.Equal(Now, row.RevokedAt);
    }

    [Fact]
    public async Task IsRevokedAsync_ReturnsTrue_AfterRevoke()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context);

        await service.RevokeAsync("jti-1", Now.AddHours(1));

        Assert.True(await service.IsRevokedAsync("jti-1"));
    }

    [Fact]
    public async Task IsRevokedAsync_ReturnsFalse_WhenTokenExpired()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context);

        await service.RevokeAsync("jti-1", Now.AddHours(-1));

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
    public async Task RevokeAsync_DoesNotDuplicate_WhenAlreadyRevoked()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context);

        await service.RevokeAsync("jti-1", Now.AddHours(1));
        await service.RevokeAsync("jti-1", Now.AddHours(1));

        Assert.Equal(1, await context.Set<RevokedToken>().CountAsync());
    }

    private static TokenRevocationService CreateService(ENoteContext context) =>
        new(context, new FixedClock(Now), new MemoryCache(new MemoryCacheOptions()));
}
