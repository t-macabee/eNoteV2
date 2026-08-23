namespace eNote.Domain.Entities.Communication;

public class RentalNotificationOutbox : AuditableEntity
{

    public string MessageType { get; set; } = "RentalStatusChanged";

    public string PayloadJson { get; set; } = null!;
    public DateTime? PublishedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}