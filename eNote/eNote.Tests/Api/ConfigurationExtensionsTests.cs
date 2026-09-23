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
        Assert.Contains("Smtp__Port", exception.Message);
        Assert.Contains("Smtp__EnableSsl", exception.Message);
    }

    [Fact]
    public void ValidateRequiredSettings_Passes_WhenSmtpPresent()
    {
        var configuration = CreateConfiguration(includeSmtp: true);

        var exception = Record.Exception(() => configuration.ValidateRequiredSettings());

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("ThisIsASecretKeyThatIsAtLeast32CharactersLong!")]
    [InlineData("your-super-secret-jwt-key-change-this-in-production-min-32-chars")]
    public void ValidateRequiredSettings_ThrowsListingJwtKey_WhenJwtKeyIsPlaceholder(string jwtKey)
    {
        var configuration = CreateConfiguration(includeSmtp: true, jwtKey: jwtKey);

        var exception = Assert.Throws<InvalidOperationException>(() => configuration.ValidateRequiredSettings());

        Assert.Contains("JWT__Key", exception.Message);
    }

    [Theory]
    [InlineData("sk_test_fake_local_dev_placeholder_not_a_real_key", "whsec_123", "Stripe__SecretKey")]
    [InlineData("sk_test_...", "whsec_123", "Stripe__SecretKey")]
    [InlineData("sk_test_123", "whsec_fake_local_dev_placeholder_not_a_real_secret", "Stripe__WebhookSecret")]
    [InlineData("sk_test_123", "whsec_...", "Stripe__WebhookSecret")]
    public void ValidateRequiredSettings_ThrowsListingStripeKey_WhenStripeValueIsPlaceholder(
        string stripeSecretKey,
        string stripeWebhookSecret,
        string expectedError)
    {
        var configuration = CreateConfiguration(
            includeSmtp: true,
            stripeSecretKey: stripeSecretKey,
            stripeWebhookSecret: stripeWebhookSecret);

        var exception = Assert.Throws<InvalidOperationException>(() => configuration.ValidateRequiredSettings());

        Assert.Contains(expectedError, exception.Message);
    }

    [Fact]
    public void ValidateRequiredSettings_ThrowsListingStripeCurrencyAndDescriptor_WhenMissing()
    {
        var configuration = CreateConfiguration(includeSmtp: true, includeStripeDisplay: false);

        var exception = Assert.Throws<InvalidOperationException>(() => configuration.ValidateRequiredSettings());

        Assert.Contains("Stripe__Currency", exception.Message);
        Assert.Contains("Stripe__StatementDescriptor", exception.Message);
    }

    private static IConfiguration CreateConfiguration(
        bool includeSmtp,
        string jwtKey = "test-signing-key-that-is-long-enough-123",
        string stripeSecretKey = "sk_test_123",
        string stripeWebhookSecret = "whsec_123",
        bool includeStripeDisplay = true)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=eNote;",
            ["Jwt:Key"] = jwtKey,
            ["Jwt:Issuer"] = "ENote.Api",
            ["Jwt:Audience"] = "ENote.Client",
            ["Stripe:SecretKey"] = stripeSecretKey,
            ["Stripe:WebhookSecret"] = stripeWebhookSecret,
            ["RabbitMQ:Host"] = "localhost",
        };

        if (includeStripeDisplay)
        {
            values["Stripe:Currency"] = "bam";
            values["Stripe:StatementDescriptor"] = "ENOTE Rental";
        }

        if (includeSmtp)
        {
            values["Smtp:Host"] = "localhost";
            values["Smtp:From"] = "noreply@example.com";
            values["Smtp:PasswordResetUrl"] = "https://localhost/reset-password";
            values["Smtp:Port"] = "1025";
            values["Smtp:EnableSsl"] = "false";
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
