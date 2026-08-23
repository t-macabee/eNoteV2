using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Contracts.Communication;
using eNote.Contracts.Lectures;

namespace eNote.Infrastructure.Messaging;

public sealed class LectureNotificationDispatcher(
    IAppDbContext context,
    IClock clock) : ILectureNotificationDispatcher
{
    public Task DispatchCancelledAsync(int lectureId, string lectureName, IReadOnlyCollection<int> enrolledStudentUserIds)
    {
        var (title, body) = ("Predavanje otkazano", $"Predavanje '{lectureName}' je otkazano.");
        var occurredAtUtc = clock.UtcNow;

        foreach (var studentUserId in enrolledStudentUserIds)
        {
            var message = new LectureCancelled(lectureId, studentUserId, lectureName, title, body, occurredAtUtc);
            NotificationOutboxWriter.Enqueue(context, NotificationMessageTypes.LectureCancelled, message);
        }

        return Task.CompletedTask;
    }
}
