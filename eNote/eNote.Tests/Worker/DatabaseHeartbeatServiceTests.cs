using eNote.Worker.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Worker;

public sealed class DatabaseHeartbeatServiceTests
{
    [Theory]
    [InlineData(HealthStatus.Healthy, true)]
    [InlineData(HealthStatus.Degraded, false)]
    [InlineData(HealthStatus.Unhealthy, false)]
    public async Task UpdateMarkerAsync_KeepsMarkerFile_OnlyWhenHealthy(HealthStatus status, bool markerExpected)
    {
        var markerPath = TempMarkerPath();
        await File.WriteAllTextAsync(markerPath, "stale");
        try
        {
            var service = CreateService(markerPath);

            await service.UpdateMarkerAsync(ReportWith(status), CancellationToken.None);

            Assert.Equal(markerExpected, File.Exists(markerPath));
        }
        finally
        {
            File.Delete(markerPath);
        }
    }

    [Fact]
    public async Task UpdateMarkerAsync_WritesMarkerToTheTempDirectory_WhenNoPathIsConfigured()
    {
        var defaultMarkerPath = Path.Combine(Path.GetTempPath(), "enote-worker-healthy");
        File.Delete(defaultMarkerPath);
        try
        {
            var service = CreateService(markerPath: null);

            await service.UpdateMarkerAsync(ReportWith(HealthStatus.Healthy), CancellationToken.None);

            Assert.Equal("Healthy", await File.ReadAllTextAsync(defaultMarkerPath));
        }
        finally
        {
            File.Delete(defaultMarkerPath);
        }
    }

    [Fact]
    public async Task ExecuteAsync_RunsFirstCheck_OnlyAfterEveryHostedServiceHasStarted()
    {
        var order = new List<string>();
        var healthChecks = new RecordingHealthCheckService(order);
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<HealthCheckService>(healthChecks);
                services.AddHostedService<DatabaseHeartbeatService>();
                services.AddHostedService(_ => new SlowStartService(order));
            })
            .Build();

        await host.StartAsync();
        await healthChecks.FirstCheck.WaitAsync(TimeSpan.FromSeconds(5));
        await host.StopAsync();

        Assert.Equal(["slow-started", "health-check"], order.Take(2));
    }

    private sealed class RecordingHealthCheckService(List<string> order) : HealthCheckService
    {
        private readonly TaskCompletionSource _first = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task FirstCheck => _first.Task;

        public override Task<HealthReport> CheckHealthAsync(
            Func<HealthCheckRegistration, bool>? predicate,
            CancellationToken cancellationToken = default)
        {
            lock (order)
            {
                order.Add("health-check");
            }

            _first.TrySetResult();
            return Task.FromResult(new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero));
        }
    }

    private sealed class SlowStartService(List<string> order) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(300, cancellationToken);
            lock (order)
            {
                order.Add("slow-started");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static DatabaseHeartbeatService CreateService(string? markerPath) =>
        new(null!,
            NullLogger<DatabaseHeartbeatService>.Instance,
            new ConfigurationBuilder()
                .AddInMemoryCollection(markerPath is null
                    ? []
                    : new Dictionary<string, string?> { ["Worker:HealthMarkerPath"] = markerPath })
                .Build(),
            null!);

    private static HealthReport ReportWith(HealthStatus status) =>
        new(new Dictionary<string, HealthReportEntry>
        {
            ["database"] = new HealthReportEntry(status, null, TimeSpan.Zero, null, new Dictionary<string, object>())
        }, TimeSpan.Zero);

    private static string TempMarkerPath() => Path.Combine(Path.GetTempPath(), $"enote-worker-healthy-test-{Guid.NewGuid():N}");
}
