using eNote.Application.Common.Persistence;
using eNote.Contracts.Rentals;
using eNote.Domain.Entities.Communication;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace eNote.Worker.Consumers;

public sealed class RentalStatusChangedConsumer(IAppDbContext dbContext, ILogger<RentalStatusChangedConsumer> logger) : IConsumer<RentalStatusChanged>
{
    public async Task Consume(ConsumeContext<RentalStatusChanged> context)
    {
        var message = context.Message;

        var notification = new Notification(message.StudentUserId, message.Title, message.Body, message.OccurredAtUtc, message.RentalId);

        dbContext.Set<Notification>().Add(notification);

        try
        {
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Notification_UserId_RentalId_CreatedAt"
        })
        {
            logger.LogWarning("Skipping duplicate rental notification for rental {RentalId} and user {UserId}.", message.RentalId, message.StudentUserId);
            return;
        }

        logger.LogInformation("Stored rental notification {NotificationId} for rental {RentalId} and user {UserId}.", notification.Id, message.RentalId, message.StudentUserId);
    }
}
