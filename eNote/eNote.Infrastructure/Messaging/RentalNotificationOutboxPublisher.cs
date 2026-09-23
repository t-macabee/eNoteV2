using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Contracts.Assignments;
using eNote.Contracts.Communication;
using eNote.Contracts.Lectures;
using eNote.Contracts.Rentals;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace eNote.Infrastructure.Messaging;

public sealed class RentalNotificationOutboxPublisher(IServiceProvider services, ILogger<RentalNotificationOutboxPublisher> logger, TimeSpan? interval = null) : BackgroundService
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
        try
        {
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
        catch (OperationCanceledException) { }
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
                object payload = message.MessageType switch
                {
                    NotificationMessageTypes.RentalStatusChanged => JsonSerializer.Deserialize<RentalStatusChanged>(message.PayloadJson, JsonOptions)!,
                    NotificationMessageTypes.LectureCancelled => JsonSerializer.Deserialize<LectureCancelled>(message.PayloadJson, JsonOptions)!,
                    NotificationMessageTypes.SubmissionGraded => JsonSerializer.Deserialize<SubmissionGraded>(message.PayloadJson, JsonOptions)!,
                    NotificationMessageTypes.PaymentRefunded => JsonSerializer.Deserialize<RentalRefunded>(message.PayloadJson, JsonOptions)!,
                    NotificationMessageTypes.AnnouncementPublished => JsonSerializer.Deserialize<AnnouncementPublished>(message.PayloadJson, JsonOptions)!,
                    _ => throw new InvalidOperationException($"Unknown notification outbox message type '{message.MessageType}'.")
                };

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
