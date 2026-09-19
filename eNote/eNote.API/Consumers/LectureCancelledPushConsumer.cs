using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using eNote.Contracts.Lectures;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Consumers;

public sealed class LectureCancelledPushConsumer(IHubContext<NotificationHub> hubContext, ILogger<LectureCancelledPushConsumer> logger)
    : NotificationPushConsumer<LectureCancelled>(hubContext, logger)
{
    protected override NotificationPush Map(LectureCancelled message) => new(
        message.StudentUserId,
        new NotificationPushDto
        {
            LectureId = message.LectureId,
            Title = message.Title,
            Body = message.Body,
            CreatedAt = message.OccurredAtUtc
        },
        "lecture-cancelled",
        $"lecture {message.LectureId}");
}
