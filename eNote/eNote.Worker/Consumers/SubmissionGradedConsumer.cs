using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Contracts.Assignments;
using eNote.Domain.Entities.Communication;
using MassTransit;

namespace eNote.Worker.Consumers;

public sealed class SubmissionGradedConsumer(IAppDbContext dbContext, ILogger<SubmissionGradedConsumer> logger)
    : NotificationPersistenceConsumer<SubmissionGraded>(dbContext, logger)
{
    protected override NotificationWrite Map(SubmissionGraded message) => new(
        new Notification(message.StudentUserId, message.Title, message.Body, message.OccurredAtUtc, submissionId: message.SubmissionId),
        "submission-graded",
        "submission",
        message.SubmissionId,
        DbConstraintNames.NotificationUserSubmissionCreatedAtUniqueIndex);
}
