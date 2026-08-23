using eNote.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;

namespace eNote.Tests.Messaging;

public sealed class RabbitMqConfigurationTests
{

    [Fact]
    public void GetMissingConfigurationError_ReturnsError_WhenHostAndUserBothMissing()
    {
        var configuration = BuildConfiguration();

        var error = RabbitMqConfiguration.GetMissingConfigurationError(configuration);

        Assert.Equal("RabbitMQ__Host (or RabbitMQ__User)", error);
    }

    [Theory]
    [InlineData("RabbitMQ:Host", "rabbitmq")]
    [InlineData("RabbitMQ:User", "guest")]
    public void GetMissingConfigurationError_ReturnsNull_WhenEitherHostOrUserIsSet(string key, string value)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?> { [key] = value });

        var error = RabbitMqConfiguration.GetMissingConfigurationError(configuration);

        Assert.Null(error);
    }

    [Fact]
    public void GetMissingConfigurationError_ReturnsError_WhenValuesAreWhitespaceOnly()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["RabbitMQ:Host"] = "   ",
            ["RabbitMQ:User"] = ""
        });

        var error = RabbitMqConfiguration.GetMissingConfigurationError(configuration);

        Assert.NotNull(error);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?>? values = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? [])
            .Build();
}
