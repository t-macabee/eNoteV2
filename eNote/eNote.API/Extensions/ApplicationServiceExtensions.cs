using eNote.API.Services;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Courses.Services;
using eNote.Application.Features.InstrumentRentals.StateMachine;
using eNote.Application.Features.Users.Services;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Identity;
using eNote.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace eNote.API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ENoteContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsAssembly("eNote.Infrastructure")));

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddSingleton<IClock, SystemClock>();
            services.AddScoped<IUserContextResolver, UserContextResolver>();
            services.AddScoped<IRentalStateMachine, RentalStateMachine>();
            services.Scan(scan => scan
                .FromAssembliesOf(typeof(AuthService), typeof(CourseService))
                .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service") && !type.IsAbstract))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.AddMemoryCache();
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
            services.AddScoped<IRentalNotificationDispatcher, RentalNotificationDispatcher>();

            services.AddScoped<IAppDbContext>(x => x.GetRequiredService<ENoteContext>());

            return services;
        }
    }
}
