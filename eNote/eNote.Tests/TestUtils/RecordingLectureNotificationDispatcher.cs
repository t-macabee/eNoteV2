using eNote.Application.Features.Academic.Lectures.Services;

namespace eNote.Tests.TestUtils;

public sealed class RecordingLectureNotificationDispatcher : ILectureNotificationDispatcher
{
    public List<(int LectureId, string LectureName, string Reason, IReadOnlyCollection<int> EnrolledStudentUserIds)> CancelledCalls { get; } = [];

    public Task DispatchCancelledAsync(int lectureId, string lectureName, string reason, IReadOnlyCollection<int> enrolledStudentUserIds)
    {
        CancelledCalls.Add((lectureId, lectureName, reason, enrolledStudentUserIds));
        return Task.CompletedTask;
    }
}
