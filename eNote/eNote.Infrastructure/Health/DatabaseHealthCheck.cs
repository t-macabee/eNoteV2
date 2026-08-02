using eNote.Application.Common.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace eNote.Infrastructure.Health;

public sealed class DatabaseHealthCheck(IDatabaseHealthProbe databaseProbe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await databaseProbe.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("Database is reachable.")
            : HealthCheckResult.Unhealthy("Database is not reachable.");
}
