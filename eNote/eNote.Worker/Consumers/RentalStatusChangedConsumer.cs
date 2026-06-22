using eNote.Contracts.Rentals;
using eNote.Domain.Entities;
using eNote.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace eNote.Worker.Consumers;

public sealed class RentalStatusChangedConsumer(ENoteContext dbContext, ILogger<RentalStatusChangedConsumer> logger) : IConsumer<RentalStatusChanged>
{
    public async Task Consume(ConsumeContext<RentalStatusChanged> context)
    {
        var message = context.Message;

        var exists = await dbContext.Set<Notification>()
            .AnyAsync(x => x.UserId == message.StudentUserId &&
            x.RentalId == message.RentalId &&
            x.Title == message.Title, context.CancellationToken);

        if (exists)
        {
            logger.LogWarning("Skipping duplicate rental notification for rental {RentalId} and user {UserId}.", message.RentalId, message.StudentUserId);
            return;
        }

        var notification = new Notification(message.StudentUserId, message.Title, message.Body, message.OccurredAtUtc, message.RentalId);

        dbContext.Set<Notification>().Add(notification);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Stored rental notification {NotificationId} for rental {RentalId} and user {UserId}.", notification.Id, message.RentalId, message.StudentUserId);
    }
}
