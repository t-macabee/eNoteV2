namespace eNote.Application.Features.Academic.Assignments.Services;

public interface ISubmissionNotificationDispatcher
{

    Task DispatchGradedAsync(int submissionId, int studentUserId, string assignmentTitle, int grade);
}
