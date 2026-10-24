namespace eNote.Application.Features.Academic.Courses.Services;

public interface IEnrollmentNotificationDispatcher
{
    Task DispatchStatusChangedAsync(int enrollmentId, int studentUserId, string courseName, EnrollmentStatus newStatus, string? reason);
    Task DispatchTuitionPaidAsync(int enrollmentId, int studentUserId, string courseName, long amountCents, string currency, DateTime paidUntil);
    Task DispatchTuitionRefundedAsync(int enrollmentId, int studentUserId, string courseName, long amountCents, string currency);
}
