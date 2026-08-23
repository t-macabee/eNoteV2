namespace eNote.Contracts.Communication;

/// <summary>
/// Discriminator values stored in <c>RentalNotificationOutbox.MessageType</c>, telling
/// <c>RentalNotificationOutboxPublisher</c> which contract type to deserialize a queued row into
/// before publishing it. One outbox table serves every notification-triggering event in the
/// system (see eNote.Infrastructure/Messaging) — this is what lets new event types reuse the
/// existing outbox → publish → Worker-consume → SignalR-push pipeline instead of each standing
/// up its own.
/// </summary>
public static class NotificationMessageTypes
{
    public const string RentalStatusChanged = nameof(RentalStatusChanged);
    public const string LectureCancelled = nameof(LectureCancelled);
    public const string SubmissionGraded = nameof(SubmissionGraded);
}
