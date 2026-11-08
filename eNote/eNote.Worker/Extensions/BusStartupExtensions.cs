using MassTransit;

namespace eNote.Worker.Extensions;

public static class BusStartupExtensions
{
    public static IServiceCollection WaitForBusBeforeApplicationStarted(this IServiceCollection services) =>
        services.Configure<MassTransitHostOptions>(options => options.WaitUntilStarted = true);
}
