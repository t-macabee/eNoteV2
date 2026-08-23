using eNote.Application.Features.Academic.Assignments.Services;

namespace eNote.Tests.TestUtils;

public sealed class RecordingSubmissionNotificationDispatcher : ISubmissionNotificationDispatcher
{
    public List<(int SubmissionId, int StudentUserId, string AssignmentTitle, int Grade)> GradedCalls { get; } = [];

    public Task DispatchGradedAsync(int submissionId, int studentUserId, string assignmentTitle, int grade)
    {
        GradedCalls.Add((submissionId, studentUserId, assignmentTitle, grade));
        return Task.CompletedTask;
    }
}
