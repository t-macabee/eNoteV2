using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Infrastructure.Messaging;

public static class MassTransitServiceExtensions
{
    public static IServiceCollection AddRabbitMqMassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureBus = null)
    {
        services.AddMassTransit(x =>
        {
            configureBus?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(
                    RabbitMqConfiguration.GetHost(configuration),
                    RabbitMqConfiguration.GetVirtualHost(configuration),
                    h =>
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
