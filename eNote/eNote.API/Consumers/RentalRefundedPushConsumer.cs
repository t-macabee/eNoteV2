using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using eNote.Contracts.Rentals;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Consumers;

public sealed class RentalRefundedPushConsumer(IHubContext<NotificationHub> hubContext, ILogger<RentalRefundedPushConsumer> logger)
    : NotificationPushConsumer<RentalRefunded>(hubContext, logger)
{
    protected override NotificationPush Map(RentalRefunded message) => new(
        message.StudentUserId,
        new NotificationPushDto
        {
            RentalId = message.RentalId,
            Title = message.Title,
            Body = message.Body,
            CreatedAt = message.OccurredAtUtc
        },
        "rental",
        $"rental {message.RentalId}");
}
