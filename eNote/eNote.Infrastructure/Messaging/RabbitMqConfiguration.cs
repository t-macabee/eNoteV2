using Microsoft.Extensions.Configuration;

namespace eNote.Infrastructure.Messaging;

public static class RabbitMqConfiguration
{
    public static string GetHost(IConfiguration configuration) =>
        configuration["RabbitMQ:Host"] ?? "localhost";

    public static string GetVirtualHost(IConfiguration configuration) =>
        configuration["RabbitMQ:VirtualHost"] ?? "/";

    public static string GetUsername(IConfiguration configuration) =>
        configuration["RabbitMQ:Username"]
        ?? configuration["RabbitMQ:User"]
        ?? "guest";

    public static string GetPassword(IConfiguration configuration) =>
        configuration["RabbitMQ:Password"] ?? "guest";

    public static string? GetMissingConfigurationError(IConfiguration configuration) =>
        string.IsNullOrWhiteSpace(configuration["RabbitMQ:Host"]) &&
        string.IsNullOrWhiteSpace(configuration["RabbitMQ:User"])
            ? "RabbitMQ__Host (or RabbitMQ__User)"
            : null;
}
