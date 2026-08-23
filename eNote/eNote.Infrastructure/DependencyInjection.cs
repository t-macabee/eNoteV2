using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Academic.Assignments.Services;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Data.Seed;
using eNote.Infrastructure.Health;
using eNote.Infrastructure.Identity;
using eNote.Infrastructure.Messaging;
using eNote.Infrastructure.Reports;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace eNote.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureBus = null)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings__DefaultConnection is required.");

        services.AddSingleton<IClock, SystemClock>();
        services.AddMemoryCache();
        services.Scan(scan => scan
            .FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.Where(type =>
                type.Name.EndsWith("Service")
                && !type.IsAbstract
                && type != typeof(ReportService)
                && type != typeof(AuthService)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());
        services.AddDbContext<ENoteContext>(options => options.UseNpgsql(
            connectionString,
            sql => sql.MigrationsAssembly(typeof(ENoteContext).Assembly.FullName)));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<ENoteContext>());
        services.AddScoped<IMigrationRunner, MigrationRunner>();
        services.AddScoped<IDatabaseHealthProbe, DatabaseHealthProbe>();
        services.AddScoped<IRentalNotificationDispatcher, RentalNotificationDispatcher>();
        services.AddScoped<ILectureNotificationDispatcher, LectureNotificationDispatcher>();
        services.AddScoped<ISubmissionNotificationDispatcher, SubmissionNotificationDispatcher>();
        services.AddHostedService<RentalNotificationOutboxPublisher>();
        services.AddRabbitMqMassTransit(configuration, configureBus);
        services.AddInfrastructureIdentity();

        return services;
    }

    /// <summary>
    /// Registered unconditionally from <see cref="AddInfrastructure"/> (not opt-in per host): the
    /// blanket "*Service" scan above picks up <c>UserAccountService</c>, which needs
    /// <see cref="UserManager{TUser}"/> regardless of which host runs the scan. All registrations
    /// here are inert until resolved — none require host-specific types (no
    /// IHttpContextAccessor is needed just to register SignInManager, only to construct one).
    /// AddDataProtection() is included because AddDefaultTokenProviders() needs
    /// IDataProtectionProvider for DataProtectorTokenProvider — ASP.NET Core's web host
    /// (WebApplication.CreateBuilder) registers this for free, but a generic Host (the Worker) or
    /// a bare ServiceCollection does not. Safe/idempotent to call more than once (TryAdd internally).
    /// </summary>
    public static IServiceCollection AddInfrastructureIdentity(this IServiceCollection services)
    {
        services.AddDataProtection();

        // AddSignInManager needs IAuthenticationSchemeProvider (via SignInManager's
        // HttpContextAccessor-independent constructor dependencies). ASP.NET Core web hosts
        // get this from their authentication services; a generic Host (the Worker) must
        // register it explicitly or host build validation fails. TryAdd-based, so the API's
        // later AddAuthentication(...) call is unaffected.
        services.AddAuthenticationCore();

        services.AddIdentityCore<AppUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequiredLength = 8;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<ENoteContext>()
        .AddSignInManager<SignInManager<AppUser>>()
        .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection AddInfrastructureHealthChecks(this IServiceCollection services) =>
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<RabbitMqHealthCheck>("rabbitmq")
            .Services;

    public static void LoadEnvironment() => Configuration.DotEnvConfiguration.Load();

    public static async Task<IHost> InitializeDevelopmentDataAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        await services.GetRequiredService<IMigrationRunner>().MigrateAsync();
        await IdentitySeed.SeedAsync(services);
        await DevelopmentDataSeed.SeedAsync(
            services.GetRequiredService<ENoteContext>(),
            services.GetRequiredService<IClock>());

        return host;
    }
}
