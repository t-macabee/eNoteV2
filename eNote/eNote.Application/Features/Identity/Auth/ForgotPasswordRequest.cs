using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Identity.Auth;

public sealed class ForgotPasswordRequest
{
    [EmailAddress]
    public required string Email { get; set; }
}
