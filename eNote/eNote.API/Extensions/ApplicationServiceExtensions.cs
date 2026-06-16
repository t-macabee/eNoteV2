using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Courses.Services;
using eNote.Application.Features.InstrumentRentals.StateMachine;
using eNote.Application.Features.Users.Services;
using eNote.Application.Features.Users.Services.Interfaces;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Identity;

namespace eNote.API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<eNote.Application.Common.Interfaces.ICurrentUserService, eNote.API.Services.CurrentUserService>();

            services.AddSingleton<IClock, SystemClock>();
            services.AddScoped<IUserContextResolver, UserContextResolver>();
            services.AddScoped<IRentalStateMachine, RentalStateMachine>();
            services.Scan(scan => scan
                .FromAssembliesOf(typeof(AuthService), typeof(CourseService))
                .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service") && !type.IsAbstract))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.AddScoped<IAppDbContext>(x => x.GetRequiredService<ENoteContext>());

            return services;
        }
    }
}
