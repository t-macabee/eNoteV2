using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Contracts.Communication;
using eNote.Domain.Entities.Communication;
using MassTransit;

namespace eNote.Worker.Consumers;

public sealed class AnnouncementPublishedConsumer(IAppDbContext dbContext, ILogger<AnnouncementPublishedConsumer> logger)
    : NotificationPersistenceConsumer<AnnouncementPublished>(dbContext, logger)
{
    protected override NotificationWrite Map(AnnouncementPublished message) => new(
        new Notification(message.StudentUserId, message.Title, message.Body, message.OccurredAtUtc, announcementId: message.AnnouncementId),
        "announcement-published",
        "announcement",
        message.AnnouncementId,
        DbConstraintNames.NotificationUserAnnouncementCreatedAtUniqueIndex);
}
