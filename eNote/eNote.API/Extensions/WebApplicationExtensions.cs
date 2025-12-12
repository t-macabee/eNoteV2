using eNote.Infrastructure.Identity;
using Microsoft.OpenApi.Models;

namespace eNote.API.Extensions
{
    public static class WebApplicationExtensions
    {
        public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
        {
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, _, _) =>
                {
                    document.Info.Title = "eNote.API | v1";
                    document.Info.Version = "1.0.0";

                    document.Components ??= new();
                    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Authorization: Bearer {JWT token}"
                    };

                    document.SecurityRequirements.Add(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                            },
                            Array.Empty<string>()
                        }
                    });

                    return Task.CompletedTask;
                });
            });

            return services;
        }

        public static async Task SeedDevelopmentDataAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            await IdentitySeeder.SeedRolesAndUsers(scope.ServiceProvider);
        }
    }
}
