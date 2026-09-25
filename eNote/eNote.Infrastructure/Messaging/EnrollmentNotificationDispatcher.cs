using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Contracts.Communication;
using eNote.Contracts.Enrollments;

namespace eNote.Infrastructure.Messaging;

public sealed class EnrollmentNotificationDispatcher(
    IAppDbContext context,
    IClock clock) : IEnrollmentNotificationDispatcher
{
    public Task DispatchStatusChangedAsync(int enrollmentId, int studentUserId, string courseName, EnrollmentStatus newStatus, string? reason)
    {
        var (title, body) = newStatus switch
        {
            EnrollmentStatus.Active => ("Upis odobren", $"Vaš zahtjev za upis na kurs '{courseName}' je odobren."),
            EnrollmentStatus.Rejected => ("Upis odbijen", $"Vaš zahtjev za upis na kurs '{courseName}' je odbijen. Razlog: {reason}"),
            EnrollmentStatus.Completed => ("Kurs položen", $"Instruktor je označio da ste položili kurs '{courseName}'."),
            _ => throw new ArgumentOutOfRangeException(nameof(newStatus), newStatus, null)
        };

        var message = new EnrollmentStatusChanged(enrollmentId, studentUserId, title, body, clock.UtcNow);
        NotificationOutboxWriter.Enqueue(context, NotificationMessageTypes.EnrollmentStatusChanged, message);

        return Task.CompletedTask;
    }
}
