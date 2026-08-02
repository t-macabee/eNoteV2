using eNote.Application.Common.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace eNote.Worker.Health;

public sealed class WorkerHealthCheck(IDatabaseHealthProbe databaseProbe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await databaseProbe.CanConnectAsync(cancellationToken);
            return canConnect ? HealthCheckResult.Healthy("Database is reachable.") : HealthCheckResult.Unhealthy("Database is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed.", ex);
        }
    }
}
