using eNote.Application.Mapping;
using Mapster;
using MapsterMapper;

namespace eNote.API.Extensions
{
    public static class MapsterServiceExtensions
    {
        public static IServiceCollection AddMapping(this IServiceCollection services)
        {
            var config = new TypeAdapterConfig();

            MapsterConfig.RegisterMappings(config);
            config.Compile();            

            services.AddSingleton(config);
            services.AddScoped<IMapper, ServiceMapper>();

            return services;
        }
    }
}
