using eNote.Domain.Entities;
using System.Text.Json;
using eNote.Application.Common.Time;
using eNote.Contracts.Rentals;
using eNote.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eNote.Worker.Services;

public sealed class RentalNotificationOutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IClock clock,
    ILogger<RentalNotificationOutboxProcessor> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Rental notification outbox processing failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ENoteContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pending = await dbContext.Set<RentalNotificationOutbox>()
            .Where(x => x.PublishedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            logger.LogDebug("Rental notification outbox batch found no pending entries.");
            return;
        }

        var publishedCount = 0;
        var failedCount = 0;

        foreach (var entry in pending)
        {
            try
            {
                var message = JsonSerializer.Deserialize<RentalStatusChanged>(entry.PayloadJson, JsonOptions)
                    ?? throw new InvalidOperationException("Outbox payload deserialized to null.");

                await publishEndpoint.Publish(message, cancellationToken);

                entry.PublishedAt = clock.UtcNow;
                entry.LastError = null;
                publishedCount++;
            }
            catch (Exception ex)
            {
                failedCount++;
                entry.Attempts++;
                entry.LastError = ex.Message[..Math.Min(ex.Message.Length, 2000)];
                logger.LogWarning(ex, "Failed to publish outbox entry {OutboxId}.", entry.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Rental notification outbox batch processed {PendingCount} entries: {PublishedCount} published, {FailedCount} failed.",
            pending.Count,
            publishedCount,
            failedCount);
    }
}