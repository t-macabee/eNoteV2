using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Contracts.Assignments;
using eNote.Domain.Entities.Communication;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace eNote.Worker.Consumers;

public sealed class SubmissionGradedConsumer(IAppDbContext dbContext, ILogger<SubmissionGradedConsumer> logger) : IConsumer<SubmissionGraded>
{
    public async Task Consume(ConsumeContext<SubmissionGraded> context)
    {
        var message = context.Message;

        var notification = new Notification(message.StudentUserId, message.Title, message.Body, message.OccurredAtUtc, submissionId: message.SubmissionId);

        dbContext.Set<Notification>().Add(notification);

        try
        {
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: DbConstraintNames.NotificationUserSubmissionCreatedAtUniqueIndex
        })
        {
            logger.LogWarning("Skipping duplicate submission-graded notification for submission {SubmissionId} and user {UserId}.", message.SubmissionId, message.StudentUserId);
            return;
        }

        logger.LogInformation("Stored submission-graded notification {NotificationId} for submission {SubmissionId} and user {UserId}.", notification.Id, message.SubmissionId, message.StudentUserId);
    }
}
