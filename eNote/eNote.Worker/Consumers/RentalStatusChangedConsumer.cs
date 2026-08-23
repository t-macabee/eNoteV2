using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Contracts.Rentals;
using eNote.Domain.Entities.Communication;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
        catch (DbUpdateException ex) when (ex.InnerException is SqlException
        {
            Number: 2601 or 2627
        } sqlEx && sqlEx.Message.Contains(DbConstraintNames.NotificationUserRentalCreatedAtUniqueIndex))
        {
            logger.LogWarning("Skipping duplicate rental notification for rental {RentalId} and user {UserId}.", message.RentalId, message.StudentUserId);
            return;
        }

        logger.LogInformation("Stored rental notification {NotificationId} for rental {RentalId} and user {UserId}.", notification.Id, message.RentalId, message.StudentUserId);
    }
}
