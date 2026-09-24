using eNote.Infrastructure.Data.Seed;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Data;

public sealed class IdentitySeedTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SeedAsync_IsIdempotent_WhenRunTwiceOnOneDatabase()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        await using var provider = SeedTestHarness.BuildProvider(context, Now);

        await IdentitySeed.SeedAsync(provider);
        await IdentitySeed.SeedAsync(provider);

        Assert.Equal(12, await context.Users.CountAsync());
    }
}
