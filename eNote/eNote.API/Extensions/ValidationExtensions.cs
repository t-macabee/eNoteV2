using eNote.Application.Features.Shared;
using FluentValidation;
using FluentValidation.AspNetCore;

namespace eNote.API.Extensions;

public static class ValidationExtensions
{
    public static IServiceCollection AddApplicationValidation(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<AnnouncementMappingConfig>();

        return services;
    }
}
