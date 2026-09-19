using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Contracts.Lectures;
using eNote.Domain.Entities.Communication;
using MassTransit;

namespace eNote.Worker.Consumers;

public sealed class LectureCancelledConsumer(IAppDbContext dbContext, ILogger<LectureCancelledConsumer> logger)
    : NotificationPersistenceConsumer<LectureCancelled>(dbContext, logger)
{
    protected override NotificationWrite Map(LectureCancelled message) => new(
        new Notification(message.StudentUserId, message.Title, message.Body, message.OccurredAtUtc, lectureId: message.LectureId),
        "lecture-cancelled",
        "lecture",
        message.LectureId,
        DbConstraintNames.NotificationUserLectureCreatedAtUniqueIndex);
}
