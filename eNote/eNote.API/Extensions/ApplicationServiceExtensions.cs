using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Identity;
using Scrutor;

namespace eNote.API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<IClock, SystemClock>();

            services.Scan(scan => scan
                .FromAssemblyOf<AuthService>()
                .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service") && !type.IsAbstract))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.AddScoped<IAppDbContext>(x => x.GetRequiredService<ENoteContext>());

            return services;
        }
    }
}
