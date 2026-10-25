using eNote.API.Extensions;
using eNote.Application.Features.Rentals.Payments.Services;
using eNote.Infrastructure;
using eNote.Infrastructure.Configuration;
using eNote.Infrastructure.Data.Seed;
using eNote.Infrastructure.Identity;
using eNote.Infrastructure.Messaging;
using eNote.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace eNote.Tests.Configuration;

public sealed class ConfigurationOptionsTests
{
    [Fact]
    public void ConnectionStringOptions_MissingDefaultConnection_Fails()
    {
        var configuration = CreateConfiguration(removeKeys: "ConnectionStrings:DefaultConnection");
        var services = new ServiceCollection();
        services.AddPersistence(configuration);

        var exception = Assert.Throws<OptionsValidationException>(() => ResolveOptions<ConnectionStringOptions>(services));

        Assert.Contains("ConnectionStrings__DefaultConnection is required.", exception.Message);
    }

    [Theory]
    [InlineData("Jwt:Key", "Jwt__Key is required.")]
    [InlineData("Jwt:Issuer", "Jwt__Issuer is required.")]
    [InlineData("Jwt:Audience", "Jwt__Audience is required.")]
    public void JwtOptions_MissingRequiredValue_Fails(string key, string message)
    {
        var services = BuildApiServices(removeKeys: key);

        var exception = Assert.Throws<OptionsValidationException>(() => ResolveOptions<JwtOptions>(services));

        Assert.Contains(message, exception.Message);
    }

    [Fact]
    public void JwtOptions_EmptySection_FailsForEveryRequiredValue()
    {
        var services = BuildApiServices(removeKeys: ["Jwt:Key", "Jwt:Issuer", "Jwt:Audience"]);

        var exception = Assert.Throws<OptionsValidationException>(() => ResolveOptions<JwtOptions>(services));

        Assert.Contains("Jwt__Key is required.", exception.Message);
        Assert.Contains("Jwt__Issuer is required.", exception.Message);
        Assert.Contains("Jwt__Audience is required.", exception.Message);
    }

    [Fact]
    public void JwtOptions_ShortKey_Fails()
    {
        var services = BuildApiServices(overrides: new Dictionary<string, string?> { ["Jwt:Key"] = "short-key" });

        var exception = Assert.Throws<OptionsValidationException>(() => ResolveOptions<JwtOptions>(services));

        Assert.Contains("Jwt__Key must be at least 32 characters.", exception.Message);
    }

    [Theory]
    [InlineData("ThisIsASecretKeyThatIsAtLeast32CharactersLong!")]
    [InlineData("your-super-secret-jwt-key-change-this-in-production-min-32-chars")]
    public void JwtOptions_PlaceholderKey_Fails(string key)
    {
        var services = BuildApiServices(overrides: new Dictionary<string, string?> { ["Jwt:Key"] = key });

        var exception = Assert.Throws<OptionsValidationException>(() => ResolveOptions<JwtOptions>(services));

        Assert.Contains("Jwt__Key is a placeholder value and must be changed before deployment.", exception.Message);
    }

    [Theory]
    [InlineData("Stripe:SecretKey", "Stripe__SecretKey is required.")]
    [InlineData("Stripe:WebhookSecret", "Stripe__WebhookSecret is required.")]
    [InlineData("Stripe:Currency", "Stripe__Currency is required.")]
    [InlineData("Stripe:StatementDescriptor", "Stripe__StatementDescriptor is required.")]
    public void StripeOptions_MissingRequiredValue_Fails(string key, string message)
    {
        var services = BuildApiServices(removeKeys: key);

        var exception = Assert.Throws<OptionsValidationException>(() => ResolveOptions<StripeOptions>(services));

        Assert.Contains(message, exception.Message);
    }

    [Fact]
    public void StripeOptions_EmptySection_FailsForEveryRequiredValue()
    {
        var services = BuildApiServices(removeKeys:
            ["Stripe:SecretKey", "Stripe:WebhookSecret", "Stripe:Currency", "Stripe:StatementDescriptor"]);

        var exception = Assert.Throws<OptionsValidationException>(() => ResolveOptions<StripeOptions>(services));

        Assert.Contains("Stripe__SecretKey is required.", exception.Message);
        Assert.Contains("Stripe__WebhookSecret is required.", exception.Message);
        Assert.Contains("Stripe__Currency is required.", exception.Message);
        Assert.Contains("Stripe__StatementDescriptor is required.", exception.Message);
    }

    [Theory]
    [InlineData("sk_test_fake_local_dev_placeholder_not_a_real_key", "whsec_123", "Stripe__SecretKey is a placeholder value")]
    [InlineData("sk_test_...", "whsec_123", "Stripe__SecretKey is a placeholder value")]
    [InlineData("sk_test_123", "whsec_fake_local_dev_placeholder_not_a_real_secret", "Stripe__WebhookSecret is a placeholder value")]
    [InlineData("sk_test_123", "whsec_...", "Stripe__WebhookSecret is a placeholder value")]
    public void StripeOptions_PlaceholderValue_Fails(string secretKey, string webhookSecret, string message)
    {
        var services = BuildApiServices(overrides: new Dictionary<string, string?>
        {
            ["Stripe:SecretKey"] = secretKey,
            ["Stripe:WebhookSecret"] = webhookSecret
        });

        var exception = Assert.Throws<OptionsValidationException>(() => ResolveOptions<StripeOptions>(services));

        Assert.Contains(message, exception.Message);
    }

    [Fact]
    public void StripeOptions_SecretKeyWithoutPrefix_Fails()
    {
        var services = BuildApiServices(overrides: new Dictionary<string, string?> { ["Stripe:SecretKey"] = "pk_test_123" });

        var exception = Assert.Throws<OptionsValidationException>(() => ResolveOptions<StripeOptions>(services));

        Assert.Contains("Stripe__SecretKey must start with sk_.", exception.Message);
    }

    [Fact]
    public void StripeOptions_Currency_IsTrimmedAndLowered()
    {
        var services = BuildApiServices(overrides: new Dictionary<string, string?> { ["Stripe:Currency"] = " BAM " });

        var options = ResolveOptions<StripeOptions>(services);

        Assert.Equal("bam", options.Currency);
    }

    [Fact]
    public void StorageOptions_MissingRoot_Fails()
    {
        var services = BuildApiServices(removeKeys: "Storage:Root");

        var exception = Assert.Throws<OptionsValidationException>(() => ResolveOptions<StorageOptions>(services));

        Assert.Contains("Storage__Root is required.", exception.Message);
    }

    [Theory]
    [InlineData("Smtp:Host", "Smtp__Host is required.")]
    [InlineData("Smtp:From", "Smtp__From is required.")]
    [InlineData("Smtp:PasswordResetUrl", "Smtp__PasswordResetUrl is required.")]
    [InlineData("Smtp:Port", "Smtp__Port must be between 1 and 65535.")]
    [InlineData("Smtp:EnableSsl", "Smtp__EnableSsl is required.")]
    public void SmtpOptions_MissingRequiredValue_Fails(string key, string message)
    {
        var services = new ServiceCollection();
        services.AddEmail(CreateConfiguration(removeKeys: key));

        var exception = Assert.Throws<OptionsValidationException>(() => ResolveOptions<SmtpOptions>(services));

        Assert.Contains(message, exception.Message);
    }

    [Fact]
    public void SmtpOptions_EmptySection_FailsForEveryRequiredValue()
    {
        var services = new ServiceCollection();
        services.AddEmail(CreateConfiguration(removeKeys:
            ["Smtp:Host", "Smtp:From", "Smtp:PasswordResetUrl", "Smtp:Port", "Smtp:EnableSsl"]));

        var exception = Assert.Throws<OptionsValidationException>(() => ResolveOptions<SmtpOptions>(services));

        Assert.Contains("Smtp__Host is required.", exception.Message);
        Assert.Contains("Smtp__From is required.", exception.Message);
        Assert.Contains("Smtp__PasswordResetUrl is required.", exception.Message);
        Assert.Contains("Smtp__Port must be between 1 and 65535.", exception.Message);
        Assert.Contains("Smtp__EnableSsl is required.", exception.Message);
    }

    [Fact]
    public void RabbitMqOptions_EmptySection_Fails()
    {
        var services = BuildMessagingServices(CreateConfiguration(
            removeKeys: ["RabbitMQ:Host", "RabbitMQ:VirtualHost", "RabbitMQ:User", "RabbitMQ:Password"]));

        var exception = Assert.Throws<OptionsValidationException>(() => ResolveOptions<RabbitMqOptions>(services));

        Assert.Contains("RabbitMQ__Host is required.", exception.Message);
        Assert.Contains("RabbitMQ__VirtualHost is required.", exception.Message);
        Assert.Contains("RabbitMQ__User is required.", exception.Message);
        Assert.Contains("RabbitMQ__Password is required.", exception.Message);
    }

    [Fact]
    public void RabbitMqOptions_ReadsEveryValueFromConfiguration()
    {
        var services = BuildMessagingServices(CreateConfiguration(new Dictionary<string, string?>
        {
            ["RabbitMQ:Host"] = "rabbitmq",
            ["RabbitMQ:VirtualHost"] = "enote",
            ["RabbitMQ:User"] = "enote-user",
            ["RabbitMQ:Password"] = "enote-password"
        }));

        var options = ResolveOptions<RabbitMqOptions>(services);

        Assert.Equal("rabbitmq", options.Host);
        Assert.Equal("enote", options.VirtualHost);
        Assert.Equal("enote-user", options.User);
        Assert.Equal("enote-password", options.Password);
    }

    [Fact]
    public void SeedOptions_MissingDefaultPassword_Fails_WhenEnabled()
    {
        var services = BuildApiServices(Environments.Development, removeKeys: "Seed:DefaultPassword");

        var exception = Assert.Throws<OptionsValidationException>(() => ResolveOptions<SeedOptions>(services));

        Assert.Contains("Seed__DefaultPassword is required.", exception.Message);
    }

    [Fact]
    public void SeedOptions_IsDisabled_OutsideDevelopment()
    {
        var services = BuildApiServices(Environments.Production, removeKeys: "Seed:DefaultPassword");

        Assert.False(ResolveOptions<SeedOptions>(services).Enabled);
    }

    [Fact]
    public void SeedOptions_ExplicitlyDisabled_PassesWithoutPassword_InDevelopment()
    {
        var services = BuildApiServices(
            Environments.Development,
            new Dictionary<string, string?> { ["Seed:Enabled"] = "false" },
            "Seed:DefaultPassword");

        Assert.False(ResolveOptions<SeedOptions>(services).Enabled);
    }

    [Fact]
    public void CorsOriginsOptions_EmptyOrigins_Fails_OutsideDevelopment()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Production));
        services.AddApplicationCors(CreateConfiguration(removeKeys: "Cors:AllowedOrigins:0"));

        var exception = Assert.Throws<OptionsValidationException>(() => ResolveOptions<CorsOriginsOptions>(services));

        Assert.Contains("Cors__AllowedOrigins is required.", exception.Message);
    }

    [Fact]
    public void CorsOriginsOptions_EmptyOrigins_Passes_InDevelopment()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Development));
        services.AddApplicationCors(CreateConfiguration(removeKeys: "Cors:AllowedOrigins:0"));

        Assert.Empty(ResolveOptions<CorsOriginsOptions>(services).AllowedOrigins);
    }

    [Fact]
    public void CorsOriginsOptions_ReadsArrayFromConfiguration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Production));
        services.AddApplicationCors(CreateConfiguration());

        Assert.Equal(["https://localhost"], ResolveOptions<CorsOriginsOptions>(services).AllowedOrigins);
    }

    private static ServiceCollection BuildApiServices(
        string? environmentName = null,
        Dictionary<string, string?>? overrides = null,
        params string[] removeKeys)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(environmentName ?? Environments.Production));
        services.AddApiInfrastructure(CreateConfiguration(overrides, removeKeys));

        return services;
    }

    private static ServiceCollection BuildMessagingServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Production));
        services.AddMessaging(configuration);

        return services;
    }

    private static T ResolveOptions<T>(IServiceCollection services) where T : class
    {
        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IOptions<T>>().Value;
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?>? overrides = null, params string[] removeKeys)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=eNote;",
            ["Jwt:Key"] = "test-signing-key-that-is-at-least-32-chars",
            ["Jwt:Issuer"] = "ENote.Api",
            ["Jwt:Audience"] = "ENote.Client",
            ["Jwt:ExpirationMinutes"] = "30",
            ["Stripe:SecretKey"] = "sk_test_123",
            ["Stripe:WebhookSecret"] = "whsec_123",
            ["Stripe:Currency"] = "bam",
            ["Stripe:StatementDescriptor"] = "ENOTE Rental",
            ["RabbitMQ:Host"] = "localhost",
            ["RabbitMQ:VirtualHost"] = "/",
            ["RabbitMQ:User"] = "guest",
            ["RabbitMQ:Password"] = "guest",
            ["Smtp:Host"] = "localhost",
            ["Smtp:Port"] = "1025",
            ["Smtp:EnableSsl"] = "false",
            ["Smtp:From"] = "noreply@example.com",
            ["Smtp:PasswordResetUrl"] = "https://localhost/reset-password",
            ["Storage:Root"] = "wwwroot",
            ["Seed:DefaultPassword"] = "test",
            ["Cors:AllowedOrigins:0"] = "https://localhost"
        };

        foreach (var key in removeKeys)
        {
            values.Remove(key);
        }

        if (overrides is not null)
        {
            foreach (var pair in overrides)
            {
                values[pair.Key] = pair.Value;
            }
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "eNote.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
