using Serilog;

namespace eNote.API.Extensions;

public static class LoggingExtensions
{
    public static IHostBuilder UseApplicationLogging(this IHostBuilder host) =>
        host.UseSerilog((ctx, services, cfg) => cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());
}
