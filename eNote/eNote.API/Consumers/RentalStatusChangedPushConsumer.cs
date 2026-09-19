using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using eNote.Contracts.Rentals;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Consumers;

public sealed class RentalStatusChangedPushConsumer(IHubContext<NotificationHub> hubContext, ILogger<RentalStatusChangedPushConsumer> logger)
    : NotificationPushConsumer<RentalStatusChanged>(hubContext, logger)
{
    protected override NotificationPush Map(RentalStatusChanged message) => new(
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
