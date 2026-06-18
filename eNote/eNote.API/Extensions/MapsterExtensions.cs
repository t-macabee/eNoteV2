using Mapster;

namespace eNote.API.Extensions;

public static class MapsterExtensions
{
    public static IServiceCollection AddMapsterMappings(this IServiceCollection services)
    {
        var config = new TypeAdapterConfig();

        config.Scan(typeof(eNote.Application.Mapping.MapsterConfig).Assembly);

        services.AddSingleton(config);
        services.AddMapster();

        return services;
    }
}
