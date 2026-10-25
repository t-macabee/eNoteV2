using eNote.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace eNote.Infrastructure.Identity;

public sealed class SmtpEmailService(IOptions<SmtpOptions> options) : IEmailService
{
    private readonly SmtpOptions _smtp = options.Value;

    public async Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        var resetLink = BuildPasswordResetLink(email, token);
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_smtp.From));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Reset lozinke";
        message.Body = new BodyBuilder
        {
            TextBody = $"Za reset lozinke otvorite: {resetLink}",
            HtmlBody = $"<p>Za reset lozinke otvorite <a href=\"{resetLink}\">ovaj link</a>.</p>"
        }.ToMessageBody();

        await SendAsync(message, cancellationToken);
    }

    public async Task SendNotificationAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_smtp.From));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            TextBody = body
        }.ToMessageBody();

        await SendAsync(message, cancellationToken);
    }

    private async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();
        var socketOptions = ResolveSocketOptions(_smtp.EnableSsl!.Value, _smtp.Port);
        await client.ConnectAsync(_smtp.Host, _smtp.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_smtp.Username))
        {
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private string BuildPasswordResetLink(string email, string token)
    {
        var separator = _smtp.PasswordResetUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{_smtp.PasswordResetUrl.TrimEnd('/')}{separator}email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }

    internal static SecureSocketOptions ResolveSocketOptions(bool enableSsl, int port)
    {
        if (!enableSsl)
        {
            return SecureSocketOptions.None;
        }

        return port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
    }
}
