using eNote.Domain.Entities.Communication;
using eNote.Infrastructure.Health;
using eNote.Infrastructure.Messaging;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace eNote.Tests.Messaging;

public sealed class OutboxHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenNoMessages()
    {
        await using var context = TestDbContextFactory.CreateContext(DateTime.UtcNow);
        var check = new OutboxHealthCheck(context);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenOnlyPendingMessages()
    {
        await using var context = TestDbContextFactory.CreateContext(DateTime.UtcNow);
        context.Set<RentalNotificationOutbox>().Add(new RentalNotificationOutbox { PayloadJson = "{}" });
        await context.SaveChangesAsync();
        var check = new OutboxHealthCheck(context);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenAbandonedMessagesExist()
    {
        await using var context = TestDbContextFactory.CreateContext(DateTime.UtcNow);
        context.Set<RentalNotificationOutbox>().Add(new RentalNotificationOutbox
        {
            PayloadJson = "{}",
            Attempts = RentalNotificationOutboxPublisher.MaxAttempts
        });
        await context.SaveChangesAsync();
        var check = new OutboxHealthCheck(context);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("1", result.Description);
    }
}
