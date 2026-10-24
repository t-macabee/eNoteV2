using eNote.Application.Common.Persistence;
using eNote.Contracts.Identity;
using eNote.Domain.Entities.Communication;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace eNote.Worker.Consumers;

public sealed class MembershipWarningConsumer(IAppDbContext dbContext, ILogger<MembershipWarningConsumer> logger)
    : IConsumer<MembershipWarning>
{
    public async Task Consume(ConsumeContext<MembershipWarning> context)
    {
        var message = context.Message;

        var stored = await dbContext.Set<Notification>().AnyAsync(
            notification => notification.UserId == message.StudentUserId
                && notification.Title == message.Title
                && notification.CreatedAt == message.OccurredAtUtc,
            context.CancellationToken);

        if (stored)
        {
            logger.LogWarning("Skipping duplicate membership warning notification for user {UserId}.", message.StudentUserId);
            return;
        }

        dbContext.Set<Notification>().Add(new Notification(message.StudentUserId, message.Title, message.Body, message.OccurredAtUtc));
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Stored membership warning notification for user {UserId}.", message.StudentUserId);
    }
}
