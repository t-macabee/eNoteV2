namespace eNote.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default);
}
