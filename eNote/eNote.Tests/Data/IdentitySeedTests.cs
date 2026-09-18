using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Infrastructure.Data.Seed;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
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
        services.AddLogging();
        services.AddOptions();
        services.AddDataProtection();
        services.AddHttpContextAccessor();
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
        services.AddScoped<IAuthenticationSchemeProvider>(_ => new StubAuthenticationSchemeProvider());
        services.AddScoped<IUserConfirmation<AppUser>>(_ => new ConfirmedUserConfirmation());

        services.AddIdentityCore<AppUser>(_ => { })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<ENoteContext>()
            .AddSignInManager<SignInManager<AppUser>>()
            .AddDefaultTokenProviders();

        return services.BuildServiceProvider();
    }

    private sealed class ConfirmedUserConfirmation : IUserConfirmation<AppUser>
    {
        public Task<bool> IsConfirmedAsync(UserManager<AppUser> manager, AppUser user) => Task.FromResult(true);
    }

    private sealed class StubAuthenticationSchemeProvider : IAuthenticationSchemeProvider
    {
        public Task<AuthenticationScheme?> GetDefaultAuthenticateSchemeAsync() => Task.FromResult<AuthenticationScheme?>(null);
        public Task<AuthenticationScheme?> GetDefaultChallengeSchemeAsync() => Task.FromResult<AuthenticationScheme?>(null);
        public Task<AuthenticationScheme?> GetDefaultForbidSchemeAsync() => Task.FromResult<AuthenticationScheme?>(null);
        public Task<AuthenticationScheme?> GetDefaultSignInSchemeAsync() => Task.FromResult<AuthenticationScheme?>(null);
        public Task<AuthenticationScheme?> GetDefaultSignOutSchemeAsync() => Task.FromResult<AuthenticationScheme?>(null);
        public Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync() => Task.FromResult<IEnumerable<AuthenticationScheme>>([]);
        public Task<AuthenticationScheme?> GetSchemeAsync(string name) => Task.FromResult<AuthenticationScheme?>(null);
        public Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync() => Task.FromResult<IEnumerable<AuthenticationScheme>>([]);
        public void AddScheme(AuthenticationScheme scheme) => throw new NotSupportedException();
        public void RemoveScheme(string name) => throw new NotSupportedException();
    }
}
