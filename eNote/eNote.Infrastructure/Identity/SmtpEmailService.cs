using eNote.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace eNote.Infrastructure.Identity;

public sealed class SmtpEmailService : IEmailService
{
    private readonly string _host;
    private readonly string _from;
    private readonly int _port;
    private readonly bool _enableSsl;
    private readonly string _passwordResetUrl;
    private readonly string? _username;
    private readonly string? _password;

    public SmtpEmailService(IOptions<SmtpOptions> options)
    {
        var smtp = options.Value;

        _host = smtp.Host;
        _from = smtp.From;
        _port = smtp.Port;
        _enableSsl = smtp.EnableSsl!.Value;
        _passwordResetUrl = smtp.PasswordResetUrl;
        _username = smtp.Username;
        _password = smtp.Password;
    }

    public async Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        var resetLink = BuildPasswordResetLink(email, token);
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_from));
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
        message.From.Add(MailboxAddress.Parse(_from));
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
        var socketOptions = ResolveSocketOptions(_enableSsl, _port);
        await client.ConnectAsync(_host, _port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_username))
        {
            await client.AuthenticateAsync(_username, _password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private string BuildPasswordResetLink(string email, string token)
    {
        var separator = _passwordResetUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{_passwordResetUrl.TrimEnd('/')}{separator}email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
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
