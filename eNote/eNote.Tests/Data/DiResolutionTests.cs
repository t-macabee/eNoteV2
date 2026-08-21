using eNote.API.Consumers;
using eNote.API.Extensions;
using eNote.Application;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Infrastructure;
using eNote.Tests.TestUtils;
using eNote.Worker;
using eNote.Worker.Consumers;
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
    /// Regression net for the API host's composition, mirroring eNote.API/Program.cs:20-30
    /// exactly (AddInfrastructure + AddJwtAuthentication + AddAuthorization +
    /// AddApplicationServices + AddMapsterMappings). Only host-level concerns are stood in:
    /// IConfiguration values (real hosts supply them via appsettings.json/environment),
    /// logging and IHostEnvironment (both hosts get these from their respective builders —
    /// WebApplication.CreateBuilder / Host.CreateApplicationBuilder — which a bare
    /// ServiceCollection does not register).
    ///
    /// Covers two failure modes:
    /// 1. A missing/unresolvable dependency, which GetRequiredService throws for directly.
    /// 2. A circular object graph routed through a factory registration (e.g. an
    ///    IAppDbContext -> ENoteContext factory delegate), which MS DI's static cycle
    ///    detector cannot see through — that fails by hanging forever instead of throwing, so
    ///    each resolution is bounded by a timeout rather than awaited directly.
    /// </summary>
    [Fact]
    public async Task ApiHostShape_AllENoteRegistrationsAreResolvable()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            // Read at registration time by AddJwtAuthentication (config["Jwt:Key"]! is
            // dereferenced while building TokenValidationParameters).
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

    /// <summary>
    /// Regression net for the Worker host's composition, mirroring eNote.Worker/Program.cs:23-24
    /// exactly: AddScoped&lt;ICurrentActor, WorkerActor&gt;() + AddInfrastructure only — no
    /// AddApplication(), no Mapster, no auth. Asserts full resolve-all success: since
    /// ReportService/AuthService were narrowed to API-only registration, nothing left in the
    /// Infrastructure scan requires API-only dependencies.
    /// </summary>
    [Fact]
    public async Task WorkerHostShape_AllENoteRegistrationsAreResolvable()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = CreateConfiguration(new Dictionary<string, string?>());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment());

        services.AddScoped<ICurrentActor, WorkerActor>();
        services.AddInfrastructure(configuration, bus => bus.AddConsumer<RentalStatusChangedConsumer>());

        await AssertAllENoteInterfacesResolvable(services);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> additional)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=x;",
            // SmtpEmailService validates these itself at construction time (not a DI gap —
            // real hosts supply them via appsettings.json/environment variables). It is part
            // of the Infrastructure "*Service" scan, so both host shapes resolve it.
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
