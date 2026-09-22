using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace eNote.Worker.Health;

public sealed class DatabaseHeartbeatService(HealthCheckService healthChecks, ILogger<DatabaseHeartbeatService> logger, IConfiguration configuration) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly string _markerPath = configuration.GetValue<string>("Worker:HealthMarkerPath") ?? "/tmp/enote-worker-healthy";

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

            await UpdateMarkerAsync(report, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task UpdateMarkerAsync(HealthReport report, CancellationToken cancellationToken)
    {
        if (report.Status != HealthStatus.Healthy)
        {
            File.Delete(_markerPath);
            return;
        }

        var directory = Path.GetDirectoryName(_markerPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(_markerPath, report.Status.ToString(), cancellationToken);
    }
}
