using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Payments;

public sealed class PaymentOptions
{
    public const string SectionName = "Stripe";

    [Required(ErrorMessage = "Stripe__Currency is required.")]
    public string Currency { get; set; } = string.Empty;

    [Required(ErrorMessage = "Stripe__RentalStatementDescriptorSuffix is required.")]
    [RegularExpression(@"^(?=.*[A-Za-z])[A-Za-z0-9 .,&/-]{1,22}$", ErrorMessage = "Stripe__RentalStatementDescriptorSuffix must have at most 22 characters, contain a letter, and use only letters, digits, spaces and . , & / -.")]
    public string RentalStatementDescriptorSuffix { get; set; } = string.Empty;

    [Required(ErrorMessage = "Stripe__TuitionStatementDescriptorSuffix is required.")]
    [RegularExpression(@"^(?=.*[A-Za-z])[A-Za-z0-9 .,&/-]{1,22}$", ErrorMessage = "Stripe__TuitionStatementDescriptorSuffix must have at most 22 characters, contain a letter, and use only letters, digits, spaces and . , & / -.")]
    public string TuitionStatementDescriptorSuffix { get; set; } = string.Empty;
}
