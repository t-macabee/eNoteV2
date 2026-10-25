using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Academic.Assignments.Services;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Application.Features.Communication.Announcements.Services;
using eNote.Application.Features.Identity.Auth.Services;
using eNote.Application.Features.Identity.Students.Services;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Rentals.Payments.Services;
using eNote.Application.Features.Reports.Services;
using eNote.Infrastructure.Configuration;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Data.Seed;
using eNote.Infrastructure.Health;
using eNote.Infrastructure.Identity;
using eNote.Infrastructure.Messaging;
using eNote.Infrastructure.Payments.Stripe;
using eNote.Infrastructure.Reports;
using eNote.Infrastructure.Storage;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace eNote.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ConnectionStringOptions>()
            .Bind(configuration.GetSection(ConnectionStringOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IClock, SystemClock>();
        services.AddDbContext<ENoteContext>((provider, options) => options.UseSqlServer(
            provider.GetRequiredService<IOptions<ConnectionStringOptions>>().Value.DefaultConnection,
            sql => sql.MigrationsAssembly(typeof(ENoteContext).Assembly.FullName)));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<ENoteContext>());
        services.AddScoped<IStoreContext>(provider => provider.GetRequiredService<ENoteContext>());
        services.AddScoped<IMigrationRunner, MigrationRunner>();
        services.AddScoped<IDatabaseHealthProbe, DatabaseHealthProbe>();

        return services;
    }

    public static IServiceCollection AddInfrastructureIdentity(this IServiceCollection services)
    {
        services.AddScoped<IUserIdentityService, UserIdentityService>();

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

    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }

    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureBus = null)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRabbitMqMassTransit(configureBus);

        return services;
    }

    public static IServiceCollection AddApiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(o => !PlaceholderSecret.IsPlaceholder(o.Key), "Jwt__Key is a placeholder value and must be changed before deployment.")
            .ValidateOnStart();

        services.AddOptions<StripeOptions>()
            .Bind(configuration.GetSection(StripeOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(o => string.IsNullOrWhiteSpace(o.SecretKey) || o.SecretKey.StartsWith("sk_", StringComparison.Ordinal), "Stripe__SecretKey must start with sk_.")
            .Validate(o => !PlaceholderSecret.IsPlaceholder(o.SecretKey), "Stripe__SecretKey is a placeholder value and must be changed before deployment.")
            .Validate(o => !PlaceholderSecret.IsPlaceholder(o.WebhookSecret), "Stripe__WebhookSecret is a placeholder value and must be changed before deployment.")
            .PostConfigure(o => o.Currency = o.Currency.Trim().ToLowerInvariant())
            .ValidateOnStart();

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SeedOptions>()
            .Configure<IHostEnvironment>((options, environment) => options.Enabled = environment.IsDevelopment())
            .Bind(configuration.GetSection(SeedOptions.SectionName))
            .Validate(o => !o.Enabled || !string.IsNullOrWhiteSpace(o.DefaultPassword), "Seed__DefaultPassword is required.")
            .ValidateOnStart();

        services.AddMemoryCache();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<ITokenRevocationService, TokenRevocationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRentalNotificationDispatcher, RentalNotificationDispatcher>();
        services.AddScoped<ILectureNotificationDispatcher, LectureNotificationDispatcher>();
        services.AddScoped<IAnnouncementNotificationDispatcher, AnnouncementNotificationDispatcher>();
        services.AddScoped<IEnrollmentNotificationDispatcher, EnrollmentNotificationDispatcher>();
        services.AddScoped<IMembershipWarningDispatcher, MembershipWarningDispatcher>();
        services.AddScoped<ISubmissionNotificationDispatcher, SubmissionNotificationDispatcher>();
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<StripeOptions>>().Value);
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        services.AddScoped<StripeWebhookService>();
        services.AddHostedService<RentalNotificationOutboxPublisher>();
        services.AddHealthChecks().AddCheck<OutboxHealthCheck>("outbox");

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
