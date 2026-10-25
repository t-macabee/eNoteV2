using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace eNote.Infrastructure.Messaging;

public static class MassTransitServiceExtensions
{
    public static IServiceCollection AddRabbitMqMassTransit(
        this IServiceCollection services,
        Action<IBusRegistrationConfigurator>? configureBus = null)
    {
        services.AddMassTransit(x =>
        {
            configureBus?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                cfg.Host(
                    options.Host,
                    options.VirtualHost,
                    h =>
                    {
                        h.Username(options.User);
                        h.Password(options.Password);
                    });

                cfg.UseMessageRetry(r => r.Exponential(4, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
