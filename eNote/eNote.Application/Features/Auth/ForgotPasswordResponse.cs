namespace eNote.Application.Features.Auth;

public sealed class ForgotPasswordResponse
{
    public string Message { get; init; } = null!;

    /// <summary>Populated only in Development for local/testing flows without SMTP.</summary>
    public string? ResetToken { get; init; }
}
