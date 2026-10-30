namespace eNote.Application.Features.Communication.Announcements.Services;

public interface IAnnouncementNotificationDispatcher
{
    Task DispatchPublishedAsync(Announcement announcement, IReadOnlyCollection<int> recipientUserIds);
}
