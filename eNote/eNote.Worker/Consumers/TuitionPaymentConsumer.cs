using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Contracts.Enrollments;
using eNote.Domain.Entities.Communication;

namespace eNote.Worker.Consumers;

public sealed class TuitionPaymentConsumer(IAppDbContext dbContext, ILogger<TuitionPaymentConsumer> logger)
    : NotificationPersistenceConsumer<TuitionPaymentNotification>(dbContext, logger)
{
    protected override NotificationWrite Map(TuitionPaymentNotification message) => new(
        new Notification(message.StudentUserId, message.Title, message.Body, message.OccurredAtUtc, enrollmentId: message.EnrollmentId),
        "tuition-payment",
        "enrollment",
        message.EnrollmentId,
        DbConstraintNames.NotificationUserEnrollmentCreatedAtUniqueIndex);
}
