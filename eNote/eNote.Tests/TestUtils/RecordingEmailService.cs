namespace eNote.Tests.TestUtils;

public sealed class RecordingEmailService : IEmailService
{
    public List<(string Email, string Token)> PasswordResets { get; } = [];
    public List<(string Email, string Subject, string Body)> Notifications { get; } = [];

    public Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        PasswordResets.Add((email, token));
        return Task.CompletedTask;
    }

    public Task SendNotificationAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        Notifications.Add((email, subject, body));
        return Task.CompletedTask;
    }
}
