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

    /// <summary>
    /// Returns the missing-setting error fragment (for aggregation into a startup
    /// validation message) if neither RabbitMQ:Host nor RabbitMQ:User is configured,
    /// or null if the configuration is sufficient. Shared by every host (API, Worker)
    /// so "is RabbitMQ configured" has a single definition instead of each host
    /// re-deriving it — and so none of them silently fall back to GetHost/GetUsername's
    /// "localhost"/"guest" defaults without at least failing fast at startup first.
    /// </summary>
    public static string? GetMissingConfigurationError(IConfiguration configuration) =>
        string.IsNullOrWhiteSpace(configuration["RabbitMQ:Host"]) &&
        string.IsNullOrWhiteSpace(configuration["RabbitMQ:User"])
            ? "RabbitMQ__Host (or RabbitMQ__User)"
            : null;
}
