using eNote.Application.Features.Academic.Lectures.Services;

namespace eNote.Tests.TestUtils;

public sealed class RecordingLectureNotificationDispatcher : ILectureNotificationDispatcher
{
    public List<(int LectureId, string LectureName, IReadOnlyCollection<int> EnrolledStudentUserIds)> CancelledCalls { get; } = [];

    public Task DispatchCancelledAsync(int lectureId, string lectureName, IReadOnlyCollection<int> enrolledStudentUserIds)
    {
        CancelledCalls.Add((lectureId, lectureName, enrolledStudentUserIds));
        return Task.CompletedTask;
    }
}
