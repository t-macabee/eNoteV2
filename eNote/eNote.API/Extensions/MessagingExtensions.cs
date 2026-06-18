using eNote.Infrastructure.Messaging;

namespace eNote.API.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddApplicationMessaging(this IServiceCollection services, IConfiguration configuration) =>
        services.AddRabbitMqMassTransit(configuration);
}
