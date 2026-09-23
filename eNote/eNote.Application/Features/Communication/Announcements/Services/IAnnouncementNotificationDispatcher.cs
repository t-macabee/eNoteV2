namespace eNote.Application.Features.Communication.Announcements.Services;

public interface IAnnouncementNotificationDispatcher
{
    Task DispatchPublishedAsync(int announcementId, string announcementTitle, IReadOnlyCollection<int> enrolledStudentUserIds);
}
