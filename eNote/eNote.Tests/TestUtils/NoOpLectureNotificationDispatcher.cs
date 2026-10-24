using eNote.Application.Features.Academic.Lectures.Services;

namespace eNote.Tests.TestUtils;

public sealed class NoOpLectureNotificationDispatcher : ILectureNotificationDispatcher
{
    public Task DispatchCancelledAsync(int lectureId, string lectureName, string reason, IReadOnlyCollection<int> enrolledStudentUserIds) => Task.CompletedTask;
}
