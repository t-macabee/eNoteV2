using eNote.Infrastructure.Messaging;
using MassTransit;

namespace eNote.API.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddApplicationMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(RabbitMqConfiguration.GetHost(configuration), RabbitMqConfiguration.GetVirtualHost(configuration), h =>
                    {
                        h.Username(RabbitMqConfiguration.GetUsername(configuration));
                        h.Password(RabbitMqConfiguration.GetPassword(configuration));
                    });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
