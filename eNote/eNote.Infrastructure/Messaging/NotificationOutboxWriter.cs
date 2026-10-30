using eNote.Application.Common.Persistence;
using eNote.Contracts.Communication;
using System.Text.Json;

namespace eNote.Infrastructure.Messaging;

internal static class NotificationOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Enqueue<TMessage>(IAppDbContext context, TMessage message)
    {
        var entry = new NotificationOutbox
        {
            MessageType = NotificationMessageTypes.NameFor(typeof(TMessage)),
            PayloadJson = JsonSerializer.Serialize(message, JsonOptions)
        };

        context.Set<NotificationOutbox>().Add(entry);
    }
}
