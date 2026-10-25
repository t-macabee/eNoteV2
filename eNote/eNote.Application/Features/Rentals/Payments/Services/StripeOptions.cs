using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Rentals.Payments.Services;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    [Required(ErrorMessage = "Stripe__SecretKey is required.")]
    public string SecretKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Stripe__WebhookSecret is required.")]
    public string WebhookSecret { get; set; } = string.Empty;

    [Required(ErrorMessage = "Stripe__Currency is required.")]
    public string Currency { get; set; } = string.Empty;

    [Required(ErrorMessage = "Stripe__StatementDescriptor is required.")]
    public string StatementDescriptor { get; set; } = string.Empty;
}
