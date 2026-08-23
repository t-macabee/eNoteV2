namespace eNote.Application.Features.Academic.Assignments.Services;

public interface ISubmissionNotificationDispatcher
{
    /// <summary>Queues a notification to the submitting student that their submission was graded.</summary>
    Task DispatchGradedAsync(int submissionId, int studentUserId, string assignmentTitle, int grade);
}
