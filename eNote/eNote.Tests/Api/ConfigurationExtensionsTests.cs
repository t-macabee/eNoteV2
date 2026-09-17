using eNote.API.Extensions;
using Microsoft.Extensions.Configuration;

namespace eNote.Tests.Api;

public sealed class ConfigurationExtensionsTests
{
    [Fact]
    public void ValidateRequiredSettings_ThrowsListingSmtpKeys_WhenSmtpMissing()
    {
        var configuration = CreateConfiguration(includeSmtp: false);

        var exception = Assert.Throws<InvalidOperationException>(() => configuration.ValidateRequiredSettings());

        Assert.Contains("Smtp__Host", exception.Message);
        Assert.Contains("Smtp__From", exception.Message);
        Assert.Contains("Smtp__PasswordResetUrl", exception.Message);
    }

    [Fact]
    public void ValidateRequiredSettings_Passes_WhenSmtpPresent()
    {
        var configuration = CreateConfiguration(includeSmtp: true);

        var exception = Record.Exception(() => configuration.ValidateRequiredSettings());

        Assert.Null(exception);
    }

    private static IConfiguration CreateConfiguration(bool includeSmtp)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=eNote;",
            ["Jwt:Key"] = "test-signing-key-that-is-long-enough-123",
            ["Jwt:Issuer"] = "ENote.Api",
            ["Jwt:Audience"] = "ENote.Client",
            ["Stripe:SecretKey"] = "sk_test_123",
            ["Stripe:WebhookSecret"] = "whsec_123",
            ["RabbitMQ:Host"] = "localhost",
        };

        if (includeSmtp)
        {
            values["Smtp:Host"] = "localhost";
            values["Smtp:From"] = "noreply@example.com";
            values["Smtp:PasswordResetUrl"] = "https://localhost/reset-password";
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
