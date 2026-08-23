using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using eNote.Contracts.Lectures;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Consumers;

public sealed class LectureCancelledPushConsumer(IHubContext<NotificationHub> hubContext, ILogger<LectureCancelledPushConsumer> logger) : IConsumer<LectureCancelled>
{
    public async Task Consume(ConsumeContext<LectureCancelled> context)
    {
        var message = context.Message;

        var payload = new NotificationPushDto()
        {
            LectureId = message.LectureId,
            Title = message.Title,
            Body = message.Body,
            CreatedAt = message.OccurredAtUtc
        };

        await hubContext.Clients.Group(NotificationHub.UserGroup(message.StudentUserId)).SendAsync(NotificationHub.ReceiveMethod, payload, context.CancellationToken);

        logger.LogInformation("Pushed lecture-cancelled notification to SignalR group for user {UserId}, lecture {LectureId}.", message.StudentUserId, message.LectureId);
    }
}
