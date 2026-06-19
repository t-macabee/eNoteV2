using eNote.API.Hubs;
using eNote.Application.Features.Notifications;
using eNote.Contracts.Rentals;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Consumers;

public sealed class RentalStatusChangedPushConsumer(IHubContext<NotificationHub> hubContext, ILogger<RentalStatusChangedPushConsumer> logger) : IConsumer<RentalStatusChanged>
{
    public async Task Consume(ConsumeContext<RentalStatusChanged> context)
    {
        RentalStatusChanged message = context.Message;

        NotificationPushDto payload = new()
        {
            RentalId = message.RentalId,
            Title = message.Title,
            Body = message.Body,
            CreatedAt = message.OccurredAtUtc
        };

        await hubContext.Clients
            .Group(NotificationHub.UserGroup(message.StudentUserId))
            .SendAsync(NotificationHub.ReceiveMethod, payload, context.CancellationToken);

        logger.LogInformation("Pushed rental notification to SignalR group for user {UserId}, rental {RentalId}.", message.StudentUserId, message.RentalId);
    }
}
