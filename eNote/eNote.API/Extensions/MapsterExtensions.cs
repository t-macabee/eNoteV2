using eNote.Application.Features.Communication.Announcements;
using Mapster;

namespace eNote.API.Extensions;

public static class MapsterExtensions
{
    public static IServiceCollection AddMapsterMappings(this IServiceCollection services)
    {
        var config = new TypeAdapterConfig();

        config.Scan(typeof(AnnouncementMappingConfig).Assembly);
        config.Compile();

        services.AddSingleton(config);
        services.AddMapster();

        return services;
    }
}
