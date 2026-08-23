namespace eNote.Application.Features.Academic.Lectures.Services;

public interface ILectureNotificationDispatcher
{
    /// <summary>Queues one notification per currently-enrolled student for a cancelled lecture.</summary>
    Task DispatchCancelledAsync(int lectureId, string lectureName, IReadOnlyCollection<int> enrolledStudentUserIds);
}
