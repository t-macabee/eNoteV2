using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Contracts.Communication;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace eNote.Infrastructure.Messaging;

public sealed class NotificationOutboxPublisher(IServiceProvider services, ILogger<NotificationOutboxPublisher> logger, TimeSpan? interval = null) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly TimeSpan _interval = interval ?? Interval;

    internal const int MaxAttempts = 5;
    private const int BatchSize = 50;
    private const int MaxStoredErrorLength = 2000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox poll failed; retrying next tick");
            }
        }
    }

    internal async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var messages = await db.Set<NotificationOutbox>()
            .Where(x => x.PublishedAt == null && x.Attempts < MaxAttempts)
            .OrderBy(x => x.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (messages.Count == 0) return;

        foreach (var message in messages)
        {
            try
            {
                var payloadType = NotificationMessageTypes.TypeFor(message.MessageType);
                object payload = JsonSerializer.Deserialize(message.PayloadJson, payloadType, JsonOptions)!;

                await publisher.Publish(payload, ct);
                message.PublishedAt = clock.UtcNow;
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.LastError = ex.Message.Length > MaxStoredErrorLength ? ex.Message[..MaxStoredErrorLength] : ex.Message;
                logger.LogError(ex, "Failed to publish outbox message {Id}", message.Id);
            }
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist outbox batch");
            throw;
        }
    }
}
