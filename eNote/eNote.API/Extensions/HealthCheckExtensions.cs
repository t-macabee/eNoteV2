using eNote.API.Health;

namespace eNote.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<RabbitMqHealthCheck>("rabbitmq");

        return services;
    }
}
