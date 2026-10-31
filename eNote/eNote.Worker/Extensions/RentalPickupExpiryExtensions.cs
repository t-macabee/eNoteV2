using eNote.Application;
using eNote.Application.Common.Interfaces;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Infrastructure.Messaging;
using eNote.Worker.Rentals;

namespace eNote.Worker.Extensions;

public static class RentalPickupExpiryExtensions
{
    public static IServiceCollection AddRentalPickupExpiry(this IServiceCollection services)
    {
        services.AddMapsterMappings();
        services.AddScoped<IRentalNotificationDispatcher, RentalNotificationDispatcher>();
        services.AddScoped<IUserProfileLookup, UserProfileLookup>();
        services.AddScoped<IStudentContext, CurrentActor>();
        services.AddScoped<IStudentDisplayNameService, StudentDisplayNameService>();
        services.AddScoped<RentalCommandService>();
        services.AddHostedService<RentalPickupExpiryService>();

        return services;
    }
}
