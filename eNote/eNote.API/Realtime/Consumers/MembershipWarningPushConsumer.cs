using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using eNote.Contracts.Identity;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Realtime.Consumers;

public sealed class MembershipWarningPushConsumer(IHubContext<NotificationHub> hubContext, ILogger<MembershipWarningPushConsumer> logger)
    : NotificationPushConsumer<MembershipWarning>(hubContext, logger)
{
    protected override NotificationPush Map(MembershipWarning message) => new(
        message.StudentUserId,
        new NotificationPushDto
        {
            Title = message.Title,
            Body = message.Body,
            CreatedAt = message.OccurredAtUtc
        },
        "membership-warning",
        $"user {message.StudentUserId}");
}
