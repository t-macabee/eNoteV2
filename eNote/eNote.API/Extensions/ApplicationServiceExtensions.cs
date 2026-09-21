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
        // Hub DTOs bypass MVC JsonOptions (JsonStringEnumConverter/UtcDateTimeJsonConverter).
        // Any future enum or non-UTC DateTime added to a push DTO requires .AddJsonProtocol(...) or manual serialization.
        services.AddSignalR();

        services.AddScoped<ICurrentUserContext, CurrentUserService>();

        services.AddInfrastructureApplicationPorts();
        services.AddApplication();

        return services;
    }
}
