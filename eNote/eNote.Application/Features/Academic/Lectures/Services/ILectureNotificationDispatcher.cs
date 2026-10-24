namespace eNote.Application.Features.Academic.Lectures.Services;

public interface ILectureNotificationDispatcher
{

    Task DispatchCancelledAsync(int lectureId, string lectureName, string reason, IReadOnlyCollection<int> enrolledStudentUserIds);
}
