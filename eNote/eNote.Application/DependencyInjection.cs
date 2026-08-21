using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<CourseService>()
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service") && !type.IsAbstract))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddScoped<IRentalStateMachine, RentalStateMachine>();
        services.AddScoped<IUserProfileLookup, UserProfileLookup>();
        services.AddScoped<ICurrentActor, CurrentActor>();

        return services;
    }
}
