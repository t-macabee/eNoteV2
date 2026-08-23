namespace eNote.Domain.Entities.Communication;

public class RentalNotificationOutbox : AuditableEntity
{
    /// <summary>
    /// Discriminator for which contract type <see cref="PayloadJson"/> deserializes to (see
    /// <c>eNote.Contracts.Communication.NotificationMessageTypes</c> in the Contracts project —
    /// not referenced here directly, Domain takes no project references). Defaults to the
    /// original rental-only value so existing rows/tests created before this column existed keep
    /// working unchanged.
    /// </summary>
    public string MessageType { get; set; } = "RentalStatusChanged";

    public string PayloadJson { get; set; } = null!;
    public DateTime? PublishedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}