using eNote.Application.Common.Persistence;
using System.Text.Json;

namespace eNote.Infrastructure.Messaging;

/// <summary>
/// Single write path shared by every notification dispatcher (<see cref="RentalNotificationDispatcher"/>,
/// <see cref="LectureNotificationDispatcher"/>, <see cref="SubmissionNotificationDispatcher"/>) — they
/// all enqueue into the same <see cref="RentalNotificationOutbox"/> table, tagged with the contract
/// type's <c>NotificationMessageTypes</c> discriminator so <see cref="RentalNotificationOutboxPublisher"/>
/// knows which type to deserialize and publish. This is what keeps non-rental event types on the
/// existing outbox → publish → Worker-consume → SignalR-push pipeline instead of each standing up
/// its own outbox table and background publisher.
/// </summary>
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
