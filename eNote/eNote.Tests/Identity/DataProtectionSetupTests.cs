using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Infrastructure;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Xml.Linq;

namespace eNote.Tests.Identity;

public sealed class DataProtectionSetupTests
{
    [Fact]
    public void AddInfrastructureIdentity_ConfiguresSharedDataProtection()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddPersistence(configuration).AddInfrastructureIdentity();

        using var provider = services.BuildServiceProvider();

        var dataProtection = provider.GetRequiredService<IOptions<DataProtectionOptions>>().Value;
        Assert.Equal("eNote", dataProtection.ApplicationDiscriminator);

        var keyManagement = provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value;
        Assert.IsType<EntityFrameworkCoreXmlRepository<ENoteContext>>(keyManagement.XmlRepository);
    }

    [Fact]
    public async Task WorkerAndApi_ShareDataProtectionKeys_SoResetTokensCrossTheServiceBoundary()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var worker = BuildHost(databaseName);
        await using var api = BuildHost(databaseName);

        AppUser user;
        using (var scope = worker.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            user = new AppUser { UserName = "jdoe", Email = "jdoe@example.com", IsActive = true };
            var created = await userManager.CreateAsync(user, "Password1!");
            Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(error => error.Description)));
        }

        string token;
        using (var scope = worker.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var workerUser = await userManager.FindByIdAsync(user.Id.ToString());
            token = await userManager.GeneratePasswordResetTokenAsync(workerUser!);
        }

        using (var scope = api.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var apiUser = await userManager.FindByIdAsync(user.Id.ToString());

            var reset = await userManager.ResetPasswordAsync(apiUser!, token, "NewPassword1!");

            Assert.True(reset.Succeeded, string.Join("; ", reset.Errors.Select(error => error.Description)));
            Assert.True(await userManager.CheckPasswordAsync(apiUser!, "NewPassword1!"));
        }
    }

    private static ServiceProvider BuildHost(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(CreateConfiguration());
        services.AddDbContext<ENoteContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<ENoteContext>());
        services.AddScoped<IStoreContext>(provider => provider.GetRequiredService<ENoteContext>());
        services.AddScoped<ICurrentUserContext>(_ => new StubCurrentActor());

        // Keep keys local to this provider unless AddInfrastructureIdentity overrides the repository
        // with the shared database one, so the test does not depend on keys on this machine.
        services.AddDataProtection();
        services.AddOptions<KeyManagementOptions>()
            .Configure(options => options.XmlRepository = new InMemoryXmlRepository());

        services.AddInfrastructureIdentity();

        return services.BuildServiceProvider();
    }

    private sealed class InMemoryXmlRepository : IXmlRepository
    {
        private readonly List<XElement> _elements = [];

        public IReadOnlyCollection<XElement> GetAllElements() => _elements.ToArray();

        public void StoreElement(XElement element, string friendlyName) => _elements.Add(element);
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=x;",
            })
            .Build();
}
