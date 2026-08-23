using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Academic.Assignments.Services;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Application.Features.Identity.Auth.Services;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Data.Seed;
using eNote.Infrastructure.Health;
using eNote.Infrastructure.Identity;
using eNote.Infrastructure.Messaging;
using eNote.Infrastructure.Reports;
using eNote.Infrastructure.Storage;
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
        Action<IBusRegistrationConfigurator>? configureBus = null,
        bool registerNotificationOutboxPublisher = true)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings__DefaultConnection is required.");

        services.AddSingleton<IClock, SystemClock>();
        services.AddMemoryCache();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<ITokenRevocationService, TokenRevocationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IUserIdentityService, UserIdentityService>();
        services.AddDbContext<ENoteContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(typeof(ENoteContext).Assembly.FullName)));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<ENoteContext>());
        // ENoteContext owns tenant store-id resolution (it feeds its global query filters),
        // so IStoreContext is satisfied by the same scoped instance.
        services.AddScoped<IStoreContext>(provider => provider.GetRequiredService<ENoteContext>());
        services.AddScoped<IMigrationRunner, MigrationRunner>();
        services.AddScoped<IDatabaseHealthProbe, DatabaseHealthProbe>();
        services.AddScoped<IRentalNotificationDispatcher, RentalNotificationDispatcher>();
        services.AddScoped<ILectureNotificationDispatcher, LectureNotificationDispatcher>();
        services.AddScoped<ISubmissionNotificationDispatcher, SubmissionNotificationDispatcher>();
        // Only one process may drain the outbox, or concurrent pollers can publish the same row twice.
        // Worker owns this by default; the API opts out (see eNote.API/Program.cs).
        if (registerNotificationOutboxPublisher)
        {
            services.AddHostedService<RentalNotificationOutboxPublisher>();
        }
        services.AddRabbitMqMassTransit(configuration, configureBus);
        services.AddInfrastructureIdentity();

        return services;
    }

    public static IServiceCollection AddInfrastructureIdentity(this IServiceCollection services)
    {
        services.AddDataProtection();

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
