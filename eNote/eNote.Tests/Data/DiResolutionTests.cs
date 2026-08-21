using eNote.Application;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Infrastructure;
using eNote.Tests.TestUtils;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eNote.Tests.Data;

/// <summary>
/// Verifies the DI container can resolve IAppDbContext without circular dependency errors.
/// Previously, ENoteContext required ICurrentActor which required IAppDbContext -> circular.
/// CurrentActor now uses IServiceProvider to break the cycle.
/// </summary>
public sealed class DiResolutionTests
{
    [Fact]
    public void ResolveIAppDbContext_DoesNotThrowCircularDependency()
    {
        var services = new ServiceCollection();

        // Register ENoteContext with in-memory provider (no real DB needed)
        services.AddDbContext<ENoteContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Register the dependencies in the same order as the real app
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IAppDbContext>(x => x.GetRequiredService<ENoteContext>());
        services.AddScoped<ICurrentActor>(_ => new StubCurrentActor());

        var provider = services.BuildServiceProvider();

        // This should NOT throw InvalidOperationException about circular dependency
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        var exception = Record.Exception(() =>
        {
            var context = sp.GetRequiredService<IAppDbContext>();
            Assert.NotNull(context);
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Regression net for two failure modes in AddApplication + AddInfrastructure registrations,
    /// resolved in a bare host that supplies only host-level concerns (IConfiguration,
    /// ICurrentUserService, logging) — no IWebHostEnvironment, no API-registered IMemoryCache,
    /// no HttpContextAccessor:
    /// 1. A missing/unresolvable dependency, which GetRequiredService throws for directly.
    /// 2. A circular object graph routed through a factory registration (e.g. an
    ///    IAppDbContext -> ENoteContext factory delegate), which MS DI's static cycle
    ///    detector cannot see through — that fails by hanging forever instead of throwing, so
    ///    each resolution is bounded by a timeout rather than awaited directly.
    /// </summary>
    [Fact]
    public async Task InfrastructureRegistrations_AreAllResolvable()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=x;",
            // SmtpEmailService validates these itself at construction time (not a DI gap —
            // real hosts supply them via appsettings.json/environment variables).
            ["Smtp:Host"] = "localhost",
            ["Smtp:From"] = "noreply@example.com",
            ["Smtp:PasswordResetUrl"] = "https://localhost/reset-password",
        }).Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<ICurrentUserService>(_ => new StubCurrentUser());
        services.AddApplication();
        services.AddInfrastructure(configuration);

        // Mirrors eNote.API's AddMapsterMappings() (Mapster/IMapper is not part of AddApplication
        // or AddInfrastructure — it's only registered by API's own composition). Any real caller of
        // AddApplication() needs this too (e.g. RecommendationService takes IMapper), so it belongs
        // here rather than skewing this test toward a host-neutral-DI gap that doesn't exist.
        var mapsterConfig = new TypeAdapterConfig();
        mapsterConfig.Scan(typeof(eNote.Application.DependencyInjection).Assembly);
        mapsterConfig.Compile();
        services.AddSingleton(mapsterConfig);
        services.AddSingleton<IMapper>(new Mapper(mapsterConfig));

        // Mirrors eNote.API's AddJwtAuthentication(): IAuthService (Application) constructor-injects
        // the concrete SignInManager<AppUser>, whose own constructor needs IAuthenticationSchemeProvider.
        // A bare AddAuthentication() registers that without needing JWT issuer/audience/key config —
        // this test never exercises a real authentication flow, only DI construction.
        services.AddAuthentication();

        // Both real hosts get IHostEnvironment for free from their respective builders
        // (WebApplication.CreateBuilder / Host.CreateApplicationBuilder) — only this bare
        // ServiceCollection needs a stand-in, so this isn't a host-neutral-DI gap either.
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment());

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        foreach (var descriptor in services.Where(d => d.ServiceType.IsInterface))
        {
            // Only eNote-owned registrations are the regression target; resolving framework
            // plumbing (MassTransit IBus/endpoints) would block waiting for a bus that never starts.
            if (descriptor.ServiceType.ContainsGenericParameters || descriptor.ImplementationType?.Namespace?.StartsWith("eNote.", StringComparison.Ordinal) != true)
                continue;

            var serviceType = descriptor.ServiceType;
            var resolveTask = Task.Run(() => scope.ServiceProvider.GetRequiredService(serviceType));

            // 5s is generous: legitimate scoped resolution in this host measures under 150ms even
            // for the heaviest chain (ENoteContext's own dependency graph).
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

    private sealed class StubCurrentUser : ICurrentUserService
    {
        public int UserId => 1;
        public bool IsAuthenticated => true;
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "eNote.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    /// <summary>
    /// Intentionally not reusing TestUtils.StubCurrentActor: that stub's async methods throw
    /// NotSupportedException when Student/Instructor/Employee are null, which would pollute this
    /// test with unrelated entity setup. This local stub returns hardcoded values from all members,
    /// keeping the DI resolution test fully self-contained with zero domain object construction.
    /// </summary>
    private sealed class StubCurrentActor : ICurrentActor
    {
        public int UserId => 1;
        public bool IsAuthenticated => true;
        public Task<Student> GetCurrentStudentAsync()
            => Task.FromResult(new Student(1, DateTime.UtcNow));
        public Task<int> GetCurrentStudentIdAsync()
            => Task.FromResult(1);
        public Task<Instructor> GetCurrentInstructorAsync()
            => throw new NotSupportedException();
        public Task<MusicStoreEmployee> GetCurrentEmployeeAsync()
            => throw new NotSupportedException();
        public Task<int> GetCurrentStoreIdAsync(CancellationToken ct = default)
            => Task.FromResult(1);
        public int GetCurrentStoreId() => 1;
    }
}
