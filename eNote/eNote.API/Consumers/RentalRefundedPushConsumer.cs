using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using eNote.Contracts.Rentals;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Consumers;

public sealed class RentalRefundedPushConsumer(IHubContext<NotificationHub> hubContext, ILogger<RentalRefundedPushConsumer> logger) : IConsumer<RentalRefunded>
{
    public async Task Consume(ConsumeContext<RentalRefunded> context)
    {
        var message = context.Message;

        var payload = new NotificationPushDto()
        {
            RentalId = message.RentalId,
            Title = message.Title,
            Body = message.Body,
            CreatedAt = message.OccurredAtUtc
        };

        await hubContext.Clients.Group(NotificationHub.UserGroup(message.StudentUserId)).SendAsync(NotificationHub.ReceiveMethod, payload, context.CancellationToken);

        logger.LogInformation("Pushed rental notification to SignalR group for user {UserId}, rental {RentalId}.", message.StudentUserId, message.RentalId);
    }
}
