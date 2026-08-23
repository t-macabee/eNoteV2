using eNote.API.Consumers;
using eNote.API.Extensions;
using eNote.Application.Common.Persistence;
using eNote.Infrastructure;
using eNote.Worker;
using eNote.Worker.Consumers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace eNote.Tests.Data;

public sealed class DiResolutionTests
{
    [Fact]
    public void ResolveIAppDbContext_DoesNotThrowCircularDependency()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ENoteContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IAppDbContext>(x => x.GetRequiredService<ENoteContext>());
        services.AddScoped<IStoreContext>(x => x.GetRequiredService<ENoteContext>());
        services.AddScoped<ICurrentUserContext>(_ => new StubCurrentActor());

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var sp = scope.ServiceProvider;
        var exception = Record.Exception(() =>
        {
            var context = sp.GetRequiredService<IAppDbContext>();
            Assert.NotNull(context);
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task ApiHostShape_AllENoteRegistrationsAreResolvable()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-signing-key-that-is-long-enough",
            ["Jwt:Issuer"] = "https://localhost",
            ["Jwt:Audience"] = "https://localhost",
        });
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment());

        services.AddInfrastructure(configuration, bus => bus.AddConsumer<RentalStatusChangedPushConsumer>())
            .AddJwtAuthentication(configuration)
            .AddAuthorization()
            .AddApplicationServices(configuration)
            .AddMapsterMappings();

        await AssertAllENoteInterfacesResolvable(services);
    }

    [Fact]
    public async Task WorkerHostShape_AllENoteRegistrationsAreResolvable()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = CreateConfiguration([]);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment());

        services.AddScoped<ICurrentUserContext, WorkerActor>();
        services.AddInfrastructure(configuration, bus => bus.AddConsumer<RentalStatusChangedConsumer>());

        await AssertAllENoteInterfacesResolvable(services);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> additional)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=x;",
            ["Smtp:Host"] = "localhost",
            ["Smtp:From"] = "noreply@example.com",
            ["Smtp:PasswordResetUrl"] = "https://localhost/reset-password",
        };
        foreach (var pair in additional)
        {
            values[pair.Key] = pair.Value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static async Task AssertAllENoteInterfacesResolvable(ServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        foreach (var descriptor in services.Where(d => d.ServiceType.IsInterface))
        {
            if (descriptor.ServiceType.ContainsGenericParameters || descriptor.ImplementationType?.Namespace?.StartsWith("eNote.", StringComparison.Ordinal) != true)
                continue;

            var serviceType = descriptor.ServiceType;
            var resolveTask = Task.Run(() => scope.ServiceProvider.GetRequiredService(serviceType));

            Exception? exception = null;
            try
            {
                await resolveTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
                Assert.Fail($"{serviceType.FullName} did not resolve within 5s — likely a circular " +
                    "dependency hidden behind a factory registration (MS DI hangs on these instead of throwing).");
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.True(exception is null, $"{serviceType.FullName} failed to resolve: {exception}");
        }
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "eNote.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class StubCurrentActor : ICurrentUserContext
    {
        public int UserId => 1;
        public bool IsAuthenticated => true;
    }
}
