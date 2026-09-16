namespace eNote.Application.Features.Identity.Auth;

public sealed class ResetPasswordRequest
{
    public required string Email { get; set; }
    public required string Token { get; set; }
    public required string NewPassword { get; set; }
}
