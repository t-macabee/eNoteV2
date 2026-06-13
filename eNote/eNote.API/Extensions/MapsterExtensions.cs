using eNote.Application.Mapping;
using Mapster;

namespace eNote.API.Extensions
{
    public static class MapsterExtensions
    {
        public static IServiceCollection AddMapsterMappings(this IServiceCollection services)
        {
            var config = new TypeAdapterConfig();

            config.Scan(typeof(MapsterConfig).Assembly);

            services.AddSingleton(config);

            services.AddMapster();

            return services;
        }
    }
}
