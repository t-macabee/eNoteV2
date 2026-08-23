using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Contracts.Lectures;
using eNote.Domain.Entities.Communication;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace eNote.Worker.Consumers;

public sealed class LectureCancelledConsumer(IAppDbContext dbContext, ILogger<LectureCancelledConsumer> logger) : IConsumer<LectureCancelled>
{
    public async Task Consume(ConsumeContext<LectureCancelled> context)
    {
        var message = context.Message;

        var notification = new Notification(message.StudentUserId, message.Title, message.Body, message.OccurredAtUtc, lectureId: message.LectureId);

        dbContext.Set<Notification>().Add(notification);

        try
        {
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException
        {
            Number: 2601 or 2627
        } sqlEx && sqlEx.Message.Contains(DbConstraintNames.NotificationUserLectureCreatedAtUniqueIndex))
        {
            logger.LogWarning("Skipping duplicate lecture-cancelled notification for lecture {LectureId} and user {UserId}.", message.LectureId, message.StudentUserId);
            return;
        }

        logger.LogInformation("Stored lecture-cancelled notification {NotificationId} for lecture {LectureId} and user {UserId}.", notification.Id, message.LectureId, message.StudentUserId);
    }
}
