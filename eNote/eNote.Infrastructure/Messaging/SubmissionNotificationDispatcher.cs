using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Academic.Assignments.Services;
using eNote.Contracts.Assignments;
using eNote.Contracts.Communication;

namespace eNote.Infrastructure.Messaging;

public sealed class SubmissionNotificationDispatcher(
    IAppDbContext context,
    IClock clock) : ISubmissionNotificationDispatcher
{
    public Task DispatchGradedAsync(int submissionId, int studentUserId, string assignmentTitle, int grade)
    {
        var (title, body) = ("Zadaća ocijenjena", $"Vaša zadaća '{assignmentTitle}' je ocijenjena. Ocjena: {grade}.");
        var message = new SubmissionGraded(submissionId, studentUserId, assignmentTitle, grade, title, body, clock.UtcNow);

        NotificationOutboxWriter.Enqueue(context, NotificationMessageTypes.SubmissionGraded, message);

        return Task.CompletedTask;
    }
}
