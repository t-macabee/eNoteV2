using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Identity.Auth;

public sealed class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
