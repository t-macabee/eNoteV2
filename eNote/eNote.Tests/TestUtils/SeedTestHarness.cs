using eNote.Application.Common.Persistence;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Infrastructure.Data.Seed;
using eNote.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Tests.TestUtils;

public static class SeedTestHarness
{
    public static ServiceProvider BuildProvider(ENoteContext context, DateTime now)
    {
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddScoped<IAppDbContext>(_ => context);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Seed:DefaultPassword"] = "test"
        }).Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<SeedOptions>().Bind(configuration.GetSection(SeedOptions.SectionName));
        services.AddScoped<IClock>(_ => new FixedClock(now));
        services.AddScoped<IFileStorageService>(_ => new StubFileStorageService());
        services.AddScoped<ICurrentUserContext>(_ => new StubCurrentActor());
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        IdentityTestHarness.AddIdentityServices(services);

        return services.BuildServiceProvider();
    }
}
