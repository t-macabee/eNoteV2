namespace eNote.Application.Features.Academic.Courses.Services;

public interface IEnrollmentNotificationDispatcher
{
    Task DispatchStatusChangedAsync(int enrollmentId, int studentUserId, string courseName, EnrollmentStatus newStatus, string? reason);
}
