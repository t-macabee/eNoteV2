namespace eNote.Domain.Entities.Rentals;


public sealed class StripeWebhookEvent : AuditableEntity
{
    public string StripeEventId { get; private set; } = null!;
    public string Type { get; private set; } = null!;
    public string PayloadJson { get; private set; } = null!;
    public DateTime ProcessedAt { get; private set; }

    private StripeWebhookEvent()
    {
    }

    public StripeWebhookEvent(string stripeEventId, string type, string payloadJson, DateTime processedAt)
    {
        StripeEventId = stripeEventId;
        Type = type;
        PayloadJson = payloadJson;
        ProcessedAt = processedAt;
    }
}
