using eNote.API.Health;
using eNote.Application.Common.Persistence;
using eNote.Infrastructure.Health;

namespace eNote.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services.AddScoped<IDatabaseHealthProbe, DatabaseHealthProbe>();

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<RabbitMqHealthCheck>("rabbitmq");

        return services;
    }
}
