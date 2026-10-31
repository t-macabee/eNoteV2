using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Communication.Announcements.Services;
using eNote.Contracts.Communication;

namespace eNote.Infrastructure.Messaging;

public sealed class AnnouncementNotificationDispatcher(
    IAppDbContext context,
    IClock clock) : IAnnouncementNotificationDispatcher
{
    public Task DispatchPublishedAsync(Announcement announcement, IReadOnlyCollection<int> recipientUserIds)
    {
        var title = announcement.CourseId is not null ? "Nova obavijest na kursu" : "Nova obavijest prodavnice";
        var body = announcement.Title;
        var occurredAtUtc = clock.UtcNow;

        foreach (var studentUserId in recipientUserIds)
        {
            var message = new AnnouncementPublished(announcement.Id, studentUserId, title, body, occurredAtUtc);
            NotificationOutboxWriter.Enqueue(context, message);
        }

        return Task.CompletedTask;
    }
}
