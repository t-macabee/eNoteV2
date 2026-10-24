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

    public Task DispatchTuitionPaidAsync(int enrollmentId, int studentUserId, string courseName, long amountCents, string currency, DateTime paidUntil)
    {
        var body = $"Školarina za kurs '{courseName}' je plaćena. Iznos: {CurrencyAmount.Format(amountCents, currency)}. Pristup do {paidUntil:dd.MM.yyyy}.";
        return EnqueueTuitionAsync(enrollmentId, studentUserId, "Školarina plaćena", body);
    }

    public Task DispatchTuitionRefundedAsync(int enrollmentId, int studentUserId, string courseName, long amountCents, string currency)
    {
        var body = $"Uplata školarine za kurs '{courseName}' je vraćena. Iznos: {CurrencyAmount.Format(amountCents, currency)}.";
        return EnqueueTuitionAsync(enrollmentId, studentUserId, "Školarina vraćena", body);
    }

    private Task EnqueueTuitionAsync(int enrollmentId, int studentUserId, string title, string body)
    {
        var message = new TuitionPaymentNotification(enrollmentId, studentUserId, title, body, clock.UtcNow);
        NotificationOutboxWriter.Enqueue(context, NotificationMessageTypes.TuitionPayment, message);

        return Task.CompletedTask;
    }
}
