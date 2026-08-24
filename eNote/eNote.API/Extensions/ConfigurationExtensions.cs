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
        else if (jwtKey == "ThisIsASecretKeyThatIsAtLeast32CharactersLong!")
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

        if (string.IsNullOrWhiteSpace(configuration["Stripe:WebhookSecret"]))
        {
            errors.Add("Stripe__WebhookSecret");
        }

        var rabbitMqError = RabbitMqConfiguration.GetMissingConfigurationError(configuration);

        if (rabbitMqError is not null)
        {
            errors.Add(rabbitMqError);
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Missing or invalid required configuration values: " + string.Join(", ", errors));
        }
    }
}
