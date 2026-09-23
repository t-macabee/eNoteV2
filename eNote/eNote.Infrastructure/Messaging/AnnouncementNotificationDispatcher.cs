using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Communication.Announcements.Services;
using eNote.Contracts.Communication;

namespace eNote.Infrastructure.Messaging;

public sealed class AnnouncementNotificationDispatcher(
    IAppDbContext context,
    IClock clock) : IAnnouncementNotificationDispatcher
{
    public Task DispatchPublishedAsync(int announcementId, string announcementTitle, IReadOnlyCollection<int> enrolledStudentUserIds)
    {
        var (title, body) = ("Nova obavijest na kursu", announcementTitle);
        var occurredAtUtc = clock.UtcNow;

        foreach (var studentUserId in enrolledStudentUserIds)
        {
            var message = new AnnouncementPublished(announcementId, studentUserId, title, body, occurredAtUtc);
            NotificationOutboxWriter.Enqueue(context, NotificationMessageTypes.AnnouncementPublished, message);
        }

        return Task.CompletedTask;
    }
}
