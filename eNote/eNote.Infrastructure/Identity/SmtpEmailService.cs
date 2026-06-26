using System.Net;
using System.Net.Mail;
using eNote.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace eNote.Infrastructure.Identity;

public sealed class SmtpEmailService : IEmailService
{
    private readonly string _host;
    private readonly string _from;
    private readonly int _port;
    private readonly bool _enableSsl;
    private readonly string? _username;
    private readonly string? _password;

    public SmtpEmailService(IConfiguration configuration)
    {
        _host = configuration["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host is required.");
        _from = configuration["Smtp:From"] ?? throw new InvalidOperationException("Smtp:From is required.");
        _port = configuration.GetValue("Smtp:Port", 25);
        _enableSsl = configuration.GetValue("Smtp:EnableSsl", true);
        _username = configuration["Smtp:Username"];
        _password = configuration["Smtp:Password"];
    }

    public async Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage(_from, email)
        {
            Subject = "Reset lozinke",
            Body = $"Token za reset lozinke: {token}"
        };

        using var client = new SmtpClient(_host, _port)
        {
            EnableSsl = _enableSsl
        };

        if (!string.IsNullOrWhiteSpace(_username))
        {
            client.Credentials = new NetworkCredential(_username, _password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
