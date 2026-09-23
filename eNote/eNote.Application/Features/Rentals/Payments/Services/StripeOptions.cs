namespace eNote.Application.Features.Rentals.Payments.Services;

public sealed class StripeOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Stripe currency code (lowercase ISO 4217).</summary>
    public string Currency { get; set; } = string.Empty;

    public string StatementDescriptor { get; set; } = string.Empty;
}
