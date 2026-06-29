using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Identity.Auth;

public sealed class ResetPasswordRequest
{
    [EmailAddress]
    public required string Email { get; set; }
    public required string Token { get; set; }
    public required string NewPassword { get; set; }
}
