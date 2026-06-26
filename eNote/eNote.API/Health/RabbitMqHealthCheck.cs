using eNote.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace eNote.API.Health;

public sealed class RabbitMqHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = RabbitMqConfiguration.GetHost(configuration),
                VirtualHost = RabbitMqConfiguration.GetVirtualHost(configuration),
                UserName = RabbitMqConfiguration.GetUsername(configuration),
                Password = RabbitMqConfiguration.GetPassword(configuration)
            };

            await using var connection = await factory.CreateConnectionAsync(cancellationToken);

            return connection.IsOpen ? HealthCheckResult.Healthy("RabbitMQ is reachable.") : HealthCheckResult.Unhealthy("RabbitMQ connection could not be opened.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ health check failed.", ex);
        }
    }
}
