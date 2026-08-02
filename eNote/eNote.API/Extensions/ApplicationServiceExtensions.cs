using eNote.API.Services;
using eNote.Application.Common.Interfaces;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

namespace eNote.API.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddSignalR();

        services.Scan(scan => scan
            .FromAssembliesOf(typeof(CourseService))
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service") && !type.IsAbstract))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentActor, CurrentActor>();
        services.AddScoped<IUserProfileLookup, UserProfileLookup>();
        services.AddScoped<IRentalStateMachine, RentalStateMachine>();
        return services;
    }
}
