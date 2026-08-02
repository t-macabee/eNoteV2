using eNote.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
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

    public SmtpEmailService(IConfiguration configuration)
    {
        _host = configuration["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host is required.");
        _from = configuration["Smtp:From"] ?? throw new InvalidOperationException("Smtp:From is required.");
        _port = configuration.GetValue("Smtp:Port", 25);
        _enableSsl = configuration.GetValue("Smtp:EnableSsl", true);
        _passwordResetUrl = configuration["Smtp:PasswordResetUrl"] ?? throw new InvalidOperationException("Smtp:PasswordResetUrl is required.");
        _username = configuration["Smtp:Username"];
        _password = configuration["Smtp:Password"];
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

        using var client = new SmtpClient();
        var socketOptions = _enableSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None;
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
}
