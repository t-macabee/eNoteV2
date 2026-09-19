using eNote.Application.Common.Persistence;
using eNote.Domain.Entities.Communication;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace eNote.Worker.Consumers;

/// A notification row to persist: the domain entity, the unique index that
/// guards it, and the two log fields (notification kind and entity reference).
public sealed record NotificationWrite(Notification Notification, string Kind, string ReferenceLabel, object ReferenceValue, string ConstraintName);

/// Shared persistence for notification messages. Subclasses only map their
/// message; the add/save/duplicate-skip/log sequence is identical for every
/// kind.
public abstract class NotificationPersistenceConsumer<TMessage>(IAppDbContext dbContext, ILogger logger) : IConsumer<TMessage>
    where TMessage : class
{
    public async Task Consume(ConsumeContext<TMessage> context)
    {
        var write = Map(context.Message);

        dbContext.Set<Notification>().Add(write.Notification);

        try
        {
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, write.ConstraintName))
        {
            logger.LogWarning("Skipping duplicate {Kind} notification for {ReferenceLabel} {ReferenceValue} and user {UserId}.", write.Kind, write.ReferenceLabel, write.ReferenceValue, write.Notification.UserId);
            return;
        }

        logger.LogInformation("Stored {Kind} notification {NotificationId} for {ReferenceLabel} {ReferenceValue} and user {UserId}.", write.Kind, write.Notification.Id, write.ReferenceLabel, write.ReferenceValue, write.Notification.UserId);
    }

    protected abstract NotificationWrite Map(TMessage message);
}
