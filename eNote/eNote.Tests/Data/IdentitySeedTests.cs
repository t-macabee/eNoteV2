using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Infrastructure.Data.Seed;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Tests.Data;

public sealed class IdentitySeedTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SeedAsync_IsIdempotent_WhenRunTwiceOnOneDatabase()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        await using var provider = BuildProvider(context);

        await IdentitySeed.SeedAsync(provider);
        await IdentitySeed.SeedAsync(provider);

        Assert.Equal(6, await context.Users.CountAsync());
    }

    private static ServiceProvider BuildProvider(ENoteContext context)
    {
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddScoped<IAppDbContext>(_ => context);
        services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:DefaultPassword"] = "test"
            }).Build());
        services.AddScoped<IClock>(_ => new FixedClock(Now));
        services.AddScoped<IFileStorageService>(_ => new StubFileStorageService());
        services.AddScoped<ICurrentUserContext>(_ => new StubCurrentActor());
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        IdentityTestHarness.AddIdentityServices(services);

        return services.BuildServiceProvider();
    }
}
