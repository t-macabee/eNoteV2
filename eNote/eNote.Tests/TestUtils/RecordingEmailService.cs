using eNote.Application.Common.Interfaces;

namespace eNote.Tests.TestUtils;

public sealed class RecordingEmailService : IEmailService
{
    public List<(string Email, string Token)> PasswordResets { get; } = [];

    public Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        PasswordResets.Add((email, token));
        return Task.CompletedTask;
    }
}
