using eNote.Domain.Entities.Communication;
using eNote.Infrastructure.Health;
using eNote.Infrastructure.Messaging;
using eNote.Tests.TestUtils;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace eNote.Tests.Messaging;

public sealed class OutboxHealthCheckTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenNoMessages()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var check = new OutboxHealthCheck(context);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenOnlyPendingMessages()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<NotificationOutbox>().Add(new NotificationOutbox { MessageType = NotificationMessageTypes.RentalStatusChanged, PayloadJson = "{}" });
        await context.SaveChangesAsync();
        var check = new OutboxHealthCheck(context);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenAbandonedMessagesExist()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<NotificationOutbox>().Add(new NotificationOutbox
        {
            MessageType = NotificationMessageTypes.RentalStatusChanged,
            PayloadJson = "{}",
            Attempts = NotificationOutboxPublisher.MaxAttempts
        });
        await context.SaveChangesAsync();
        var check = new OutboxHealthCheck(context);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("1", result.Description);
    }
}
