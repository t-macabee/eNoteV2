using eNote.Infrastructure.Email;
using MailKit.Security;

namespace eNote.Tests.Email;

public sealed class SmtpEmailServiceTests
{
    [Fact]
    public void ResolveSocketOptions_MapsSslAndPorts()
    {
        Assert.Equal(SecureSocketOptions.None, SmtpEmailService.ResolveSocketOptions(false, 587));
        Assert.Equal(SecureSocketOptions.StartTls, SmtpEmailService.ResolveSocketOptions(true, 587));
        Assert.Equal(SecureSocketOptions.SslOnConnect, SmtpEmailService.ResolveSocketOptions(true, 465));
    }
}
