using eNote.Infrastructure.Identity;
using eNote.Infrastructure.Messaging;

namespace eNote.API.Extensions;

public static class ConfigurationExtensions
{
    public static void ValidateRequiredSettings(this IConfiguration configuration)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        {
            errors.Add("ConnectionStrings__DefaultConnection");
        }

        var jwtKey = configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            errors.Add("JWT__Key");
        }
        else if (jwtKey.Length < 32)
        {
            errors.Add("JWT__Key (minimum 32 characters)");
        }
        else if (IsPlaceholderSecret(jwtKey))
        {
            errors.Add("JWT__Key (placeholder value — must be changed before deployment)");
        }

        if (string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]))
        {
            errors.Add("JWT__Issuer");
        }

        if (string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]))
        {
            errors.Add("JWT__Audience");
        }

        var stripeSecretKey = configuration["Stripe:SecretKey"];

        if (string.IsNullOrWhiteSpace(stripeSecretKey))
        {
            errors.Add("Stripe__SecretKey");
        }
        else if (!stripeSecretKey.StartsWith("sk_", StringComparison.Ordinal))
        {
            errors.Add("Stripe__SecretKey (must start with sk_)");
        }
        else if (IsPlaceholderSecret(stripeSecretKey))
        {
            errors.Add("Stripe__SecretKey (placeholder value — must be changed before deployment)");
        }

        var stripeWebhookSecret = configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrWhiteSpace(stripeWebhookSecret))
        {
            errors.Add("Stripe__WebhookSecret");
        }
        else if (IsPlaceholderSecret(stripeWebhookSecret))
        {
            errors.Add("Stripe__WebhookSecret (placeholder value — must be changed before deployment)");
        }

        var rabbitMqError = RabbitMqConfiguration.GetMissingConfigurationError(configuration);

        if (rabbitMqError is not null)
        {
            errors.Add(rabbitMqError);
        }

        foreach (var smtpKey in SmtpEmailService.RequiredConfigurationKeys)
        {
            if (string.IsNullOrWhiteSpace(configuration[smtpKey]))
            {
                errors.Add(smtpKey.Replace(":", "__"));
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Missing or invalid required configuration values: " + string.Join(", ", errors));
        }
    }

    private static bool IsPlaceholderSecret(string value) =>
        value.Contains("change-this-in-production", StringComparison.OrdinalIgnoreCase)
        || value.Contains("fake_local_dev_placeholder", StringComparison.OrdinalIgnoreCase)
        || value == "ThisIsASecretKeyThatIsAtLeast32CharactersLong!"
        || value.StartsWith("sk_test_...", StringComparison.Ordinal)
        || value.StartsWith("whsec_...", StringComparison.Ordinal);
}
