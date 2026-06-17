using Microsoft.AspNetCore.Authorization;
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
                options.AddOperationTransformer<AnonymousOperationTransformer>();
            });

            return services;
        }
    }

    public sealed class BearerSecurityTransformer : IOpenApiDocumentTransformer
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
                Description = "Unesite važeći JSON Web Token (JWT)."
            });

            document.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                }
            ];

            return Task.CompletedTask;
        }
    }

    public sealed class AnonymousOperationTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            IList<object> metadata = context.Description.ActionDescriptor.EndpointMetadata;

            if (metadata.Any(m => m is IAllowAnonymous))
            {
                operation.Security = [];
                return Task.CompletedTask;
            }

            if (!metadata.Any(m => m is IAuthorizeData))
            {
                operation.Security = [];
            }

            return Task.CompletedTask;
        }
    }
}
