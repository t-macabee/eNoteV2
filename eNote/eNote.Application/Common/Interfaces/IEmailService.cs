namespace eNote.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default);
    Task SendNotificationAsync(string email, string subject, string body, CancellationToken cancellationToken = default);
}
