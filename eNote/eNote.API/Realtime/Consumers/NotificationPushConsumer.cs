using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Realtime.Consumers;

public sealed record NotificationPush(int StudentUserId, NotificationPushDto Payload, string Kind, string Reference);

public abstract class NotificationPushConsumer<TMessage>(IHubContext<NotificationHub> hubContext, ILogger logger) : IConsumer<TMessage>
    where TMessage : class
{
    public async Task Consume(ConsumeContext<TMessage> context)
    {
        var push = Map(context.Message);

        await hubContext.Clients.Group(NotificationHub.UserGroup(push.StudentUserId)).SendAsync(NotificationHub.ReceiveMethod, push.Payload, context.CancellationToken);

        logger.LogInformation("Pushed {Kind} notification to SignalR group for user {UserId}, {Reference}.", push.Kind, push.StudentUserId, push.Reference);
    }

    protected abstract NotificationPush Map(TMessage message);
}
