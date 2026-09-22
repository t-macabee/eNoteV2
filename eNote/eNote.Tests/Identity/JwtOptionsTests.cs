using eNote.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace eNote.Tests.Identity;

public sealed class JwtOptionsTests
{
    [Theory]
    [InlineData(4)]
    [InlineData(121)]
    public void ExpirationMinutes_FailsValidation_OutsideFiveToOneHundredTwenty(int expirationMinutes)
    {
        var options = BuildOptions(expirationMinutes);

        var exception = Assert.Throws<OptionsValidationException>(() => options.Value);

        Assert.Contains(nameof(JwtOptions.ExpirationMinutes), exception.Message);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(120)]
    public void ExpirationMinutes_PassesValidation_WithinFiveToOneHundredTwenty(int expirationMinutes)
    {
        var options = BuildOptions(expirationMinutes);

        Assert.Equal(expirationMinutes, options.Value.ExpirationMinutes);
    }

    private static IOptions<JwtOptions> BuildOptions(int expirationMinutes)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-signing-key-that-is-32-characters-long!!",
            ["Jwt:Issuer"] = "Issuer",
            ["Jwt:Audience"] = "Audience",
            ["Jwt:ExpirationMinutes"] = expirationMinutes.ToString()
        }).Build();

        var services = new ServiceCollection();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations();

        return services.BuildServiceProvider().GetRequiredService<IOptions<JwtOptions>>();
    }
}
