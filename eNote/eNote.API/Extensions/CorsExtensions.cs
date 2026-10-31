using eNote.API.Configuration;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace eNote.API.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "ENoteCors";

    public static IServiceCollection AddApplicationCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CorsOriginsOptions>()
            .Bind(configuration.GetSection(CorsOriginsOptions.SectionName))
            .Validate(o => o.AllowedOrigins.Length > 0, "Cors__AllowedOrigins is required.")
            .Validate(o => o.AllowedOrigins.All(IsOrigin), "Cors__AllowedOrigins must contain absolute http or https origins without a path.")
            .ValidateOnStart();

        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<CorsOriginsOptions>>((options, originsOptions) =>
            {
                var origins = originsOptions.Value.AllowedOrigins;

                options.AddPolicy(PolicyName, policy => policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
            });

        return services;
    }

    private static bool IsOrigin(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && uri.AbsolutePath == "/"
        && uri.Query.Length == 0;
}
