using eNote.Worker.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Worker;

public sealed class DatabaseHeartbeatServiceTests
{
    [Theory]
    [InlineData(HealthStatus.Healthy, true)]
    [InlineData(HealthStatus.Degraded, true)]
    [InlineData(HealthStatus.Unhealthy, false)]
    public async Task UpdateMarkerAsync_KeepsMarkerFile_UnlessUnhealthy(HealthStatus status, bool markerExpected)
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

    private static DatabaseHeartbeatService CreateService(string markerPath) =>
        new(null!,
            NullLogger<DatabaseHeartbeatService>.Instance,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Worker:HealthMarkerPath"] = markerPath })
                .Build());

    private static HealthReport ReportWith(HealthStatus status) =>
        new(new Dictionary<string, HealthReportEntry>
        {
            ["database"] = new HealthReportEntry(status, null, TimeSpan.Zero, null, new Dictionary<string, object>())
        }, TimeSpan.Zero);

    private static string TempMarkerPath() => Path.Combine(Path.GetTempPath(), $"enote-worker-healthy-test-{Guid.NewGuid():N}");
}
