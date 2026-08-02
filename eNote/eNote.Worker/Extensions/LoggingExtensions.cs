using Serilog;

namespace eNote.Worker.Extensions;

public static class LoggingExtensions
{
    public static IServiceCollection AddApplicationLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((serviceProvider, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(serviceProvider)
            .Enrich.FromLogContext());

        return services;
    }
}
