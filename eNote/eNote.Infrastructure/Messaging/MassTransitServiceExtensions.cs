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

                cfg.UseMessageRetry(r => r.Exponential(4, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
