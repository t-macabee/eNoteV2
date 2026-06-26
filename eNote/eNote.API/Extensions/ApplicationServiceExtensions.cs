using eNote.API.Services;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Communication.Announcements.Services;
using eNote.Application.Features.Identity.Instructors;
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
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
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

        services.AddScoped<ICourseAnnouncementService, AnnouncementService>();
        services.AddScoped<IStoreAnnouncementService, AnnouncementService>();
        services.AddScoped<IStudentAnnouncementService, AnnouncementService>();
        services.AddScoped<IAdminInstructorService, AdminInstructorService>();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUserContextResolver, UserContextResolver>();
        services.AddScoped<IRentalStateMachine, RentalStateMachine>();
        services.AddScoped<IRentalNotificationDispatcher, RentalNotificationDispatcher>();

        services.AddScoped<IAppDbContext>(x => x.GetRequiredService<ENoteContext>());

        return services;
    }
}
