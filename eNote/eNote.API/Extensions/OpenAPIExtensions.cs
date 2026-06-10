using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace eNote.API.Extensions
{
    public static class OpenAPIExtensions
    {
        public static IServiceCollection AddScalarDocumentation(this IServiceCollection services)
        {
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<BearerSecurityTransformer>();
            });

            return services;
        }
    }

    public class BearerSecurityTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();

            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your valid JSON Web Token (JWT) here."
            });

                document.Security = [new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                }
            ];

            return Task.CompletedTask;
        }
    }
}