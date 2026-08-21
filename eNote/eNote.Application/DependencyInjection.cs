using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers application-layer services (scanned <c>*Service</c> implementations,
    /// state machine, user profile lookup, current actor).
    /// </summary>
    /// <remarks>
    /// Prerequisite: the caller must register <see cref="ICurrentUserService"/> before
    /// resolution occurs. <see cref="CurrentActor"/>, registered here as
    /// <see cref="ICurrentActor"/>, requires it via constructor injection, but this
    /// method does not register it itself — hosts without an HTTP request context
    /// (e.g. a worker) should supply their own implementation.
    /// </remarks>
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
