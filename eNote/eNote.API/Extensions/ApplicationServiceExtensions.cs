using eNote.API.Services;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Identity;
using eNote.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace eNote.API.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ENoteContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly("eNote.Infrastructure")));

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddSingleton<IClock, SystemClock>();

        services.Scan(scan => scan
            .FromAssembliesOf(typeof(AuthService), typeof(CourseService))
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service") && !type.IsAbstract))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentActor, CurrentActor>();
        services.AddScoped<IUserProfileLookup, UserProfileLookup>();
        services.AddScoped<IRentalStateMachine, RentalStateMachine>();
        services.AddScoped<IRentalNotificationDispatcher, RentalNotificationDispatcher>();

        services.AddScoped<IAppDbContext>(x => x.GetRequiredService<ENoteContext>());
        services.AddScoped<IMigrationRunner, MigrationRunner>();
        services.AddHostedService<RentalNotificationOutboxPublisher>();

        return services;
    }
}
