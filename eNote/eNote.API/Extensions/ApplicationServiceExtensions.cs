using eNote.API.Services;
using eNote.Application;
using eNote.Application.Common.Interfaces;
using eNote.Infrastructure;

namespace eNote.API.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSignalR();

        services.AddScoped<ICurrentUserContext, CurrentUserService>();

        services.AddInfrastructureApplicationPorts();
        services.AddApplication();

        return services;
    }
}
