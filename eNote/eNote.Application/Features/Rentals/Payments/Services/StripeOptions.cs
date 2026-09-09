namespace eNote.Application.Features.Rentals.Payments.Services;

public sealed class StripeOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Stripe currency code (lowercase ISO 4217). Defaults to "bam".</summary>
    public string Currency { get; set; } = "bam";

    public string StatementDescriptor { get; set; } = "ENOTE Rental";
}
