using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Contracts.Enrollments;
using eNote.Domain.Entities.Communication;

namespace eNote.Worker.Consumers;

public sealed class InstructorEnrollmentNotificationConsumer(IAppDbContext dbContext, ILogger<InstructorEnrollmentNotificationConsumer> logger)
    : NotificationPersistenceConsumer<InstructorEnrollmentNotification>(dbContext, logger)
{
    protected override NotificationWrite Map(InstructorEnrollmentNotification message) => new(
        new Notification(message.InstructorUserId, message.Title, message.Body, message.OccurredAtUtc, enrollmentId: message.EnrollmentId, courseId: message.CourseId),
        "instructor-enrollment",
        "enrollment",
        message.EnrollmentId,
        DbConstraintNames.NotificationUserEnrollmentCreatedAtUniqueIndex);
}
