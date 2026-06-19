namespace eNote.API.Extensions;

public static class SignalRExtensions
{
    public static IServiceCollection AddApplicationSignalR(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }
}
