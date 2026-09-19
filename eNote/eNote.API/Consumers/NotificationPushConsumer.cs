using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Consumers;

/// A mapped notification: the target user, the SignalR payload, and the two
/// log fields (notification kind and the entity reference).
public sealed record NotificationPush(int StudentUserId, NotificationPushDto Payload, string Kind, string Reference);

/// Shared SignalR push for notification messages. Subclasses only map their
/// message; the group send and the log line are identical for every kind.
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
