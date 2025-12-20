using eNote.Application.Interfaces;
using eNote.Application.Interfaces.Abstractions;
using eNote.Application.Services;
using eNote.Infrastructure.Data.Context;
using eNote.Infrastructure.Identity;
using Microsoft.OpenApi.Models;

namespace eNote.API.Extensions
{
    public static class ServiceCollectionExtensions
    {       
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUserService, UserService>();

            services.AddScoped<IUserIdentityService, UserIdentityService>();
            services.AddScoped<IAppDbContext>(x => x.GetRequiredService<ENoteContext>());

            return services;
        }

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
    }
}
