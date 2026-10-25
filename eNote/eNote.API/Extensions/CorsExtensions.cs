using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace eNote.API.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "ENoteCors";

    public static IServiceCollection AddApplicationCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CorsOriginsOptions>()
            .Bind(configuration.GetSection(CorsOriginsOptions.SectionName))
            .Validate<IHostEnvironment>((options, environment) => environment.IsDevelopment() || options.AllowedOrigins.Length > 0, "Cors__AllowedOrigins is required.")
            .ValidateOnStart();

        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<CorsOriginsOptions>>((options, originsOptions) =>
            {
                var origins = originsOptions.Value.AllowedOrigins;

                options.AddPolicy(PolicyName, policy =>
                {
                    if (origins.Length == 0)
                    {
                        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                        return;
                    }

                    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                });
            });

        return services;
    }
}
