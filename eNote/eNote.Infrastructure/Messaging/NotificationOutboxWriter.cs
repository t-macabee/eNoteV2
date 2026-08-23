using eNote.Application.Common.Persistence;
using System.Text.Json;

namespace eNote.Infrastructure.Messaging;

internal static class NotificationOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Enqueue<TMessage>(IAppDbContext context, string messageType, TMessage message)
    {
        var entry = new RentalNotificationOutbox
        {
            MessageType = messageType,
            PayloadJson = JsonSerializer.Serialize(message, JsonOptions)
        };

        context.Set<RentalNotificationOutbox>().Add(entry);
    }
}
