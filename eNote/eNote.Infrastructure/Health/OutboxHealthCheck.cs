using eNote.Application.Common.Persistence;
using eNote.Domain.Entities.Communication;
using eNote.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace eNote.Infrastructure.Health;

public sealed class OutboxHealthCheck(IAppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var abandoned = await dbContext.Set<NotificationOutbox>()
            .CountAsync(x => x.PublishedAt == null && x.Attempts >= RentalNotificationOutboxPublisher.MaxAttempts, cancellationToken);

        return abandoned == 0
            ? HealthCheckResult.Healthy("No abandoned outbox messages.")
            : HealthCheckResult.Degraded($"{abandoned} outbox message(s) exceeded max publish attempts.");
    }
}
