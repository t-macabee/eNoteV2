using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using eNote.Contracts.Communication;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Realtime.Consumers;

public sealed class AnnouncementPublishedPushConsumer(IHubContext<NotificationHub> hubContext, ILogger<AnnouncementPublishedPushConsumer> logger)
    : NotificationPushConsumer<AnnouncementPublished>(hubContext, logger)
{
    protected override NotificationPush Map(AnnouncementPublished message) => new(
        message.StudentUserId,
        new NotificationPushDto
        {
            AnnouncementId = message.AnnouncementId,
            Title = message.Title,
            Body = message.Body,
            CreatedAt = message.OccurredAtUtc
        },
        "announcement-published",
        $"announcement {message.AnnouncementId}");
}
