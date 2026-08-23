using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Contracts.Assignments;
using eNote.Domain.Entities.Communication;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
        catch (DbUpdateException ex) when (ex.InnerException is SqlException
        {
            Number: 2601 or 2627
        } sqlEx && sqlEx.Message.Contains(DbConstraintNames.NotificationUserSubmissionCreatedAtUniqueIndex))
        {
            logger.LogWarning("Skipping duplicate submission-graded notification for submission {SubmissionId} and user {UserId}.", message.SubmissionId, message.StudentUserId);
            return;
        }

        logger.LogInformation("Stored submission-graded notification {NotificationId} for submission {SubmissionId} and user {UserId}.", notification.Id, message.SubmissionId, message.StudentUserId);
    }
}
