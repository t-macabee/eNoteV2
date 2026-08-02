using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace eNote.Worker.Health;

public sealed class DatabaseHeartbeatService(HealthCheckService healthChecks, ILogger<DatabaseHeartbeatService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            var report = await healthChecks.CheckHealthAsync(stoppingToken);

            if (report.Status == HealthStatus.Healthy)
            {
                logger.LogInformation("Worker database heartbeat is healthy.");
            }
            else
            {
                logger.LogWarning("Worker database heartbeat is {Status}.", report.Status);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
