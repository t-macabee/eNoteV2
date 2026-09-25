using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Contracts.Enrollments;
using eNote.Domain.Entities.Communication;
using MassTransit;

namespace eNote.Worker.Consumers;

public sealed class EnrollmentStatusChangedConsumer(IAppDbContext dbContext, ILogger<EnrollmentStatusChangedConsumer> logger)
    : NotificationPersistenceConsumer<EnrollmentStatusChanged>(dbContext, logger)
{
    protected override NotificationWrite Map(EnrollmentStatusChanged message) => new(
        new Notification(message.StudentUserId, message.Title, message.Body, message.OccurredAtUtc, enrollmentId: message.EnrollmentId),
        "enrollment-status-changed",
        "enrollment",
        message.EnrollmentId,
        DbConstraintNames.NotificationUserEnrollmentCreatedAtUniqueIndex);
}
