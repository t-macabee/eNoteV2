using eNote.Worker.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

    private static DatabaseHeartbeatService CreateService(string? markerPath) =>
        new(null!,
            NullLogger<DatabaseHeartbeatService>.Instance,
            new ConfigurationBuilder()
                .AddInMemoryCollection(markerPath is null
                    ? []
                    : new Dictionary<string, string?> { ["Worker:HealthMarkerPath"] = markerPath })
                .Build());

    private static HealthReport ReportWith(HealthStatus status) =>
        new(new Dictionary<string, HealthReportEntry>
        {
            ["database"] = new HealthReportEntry(status, null, TimeSpan.Zero, null, new Dictionary<string, object>())
        }, TimeSpan.Zero);

    private static string TempMarkerPath() => Path.Combine(Path.GetTempPath(), $"enote-worker-healthy-test-{Guid.NewGuid():N}");
}
