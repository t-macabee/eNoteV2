using eNote.Application.Features.Academic.Assignments.Services;

namespace eNote.Tests.TestUtils;

public sealed class NoOpSubmissionNotificationDispatcher : ISubmissionNotificationDispatcher
{
    public Task DispatchGradedAsync(int submissionId, int studentUserId, string assignmentTitle, int grade) => Task.CompletedTask;
}
