using System.ComponentModel.DataAnnotations;

namespace eNote.Infrastructure.Payments.Stripe;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    [Required(ErrorMessage = "Stripe__SecretKey is required.")]
    public string SecretKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Stripe__WebhookSecret is required.")]
    public string WebhookSecret { get; set; } = string.Empty;
}
