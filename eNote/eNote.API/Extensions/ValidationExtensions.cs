using eNote.Application.Validation.Identity;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Globalization;

namespace eNote.API.Extensions;

public static class ValidationExtensions
{
    public static IServiceCollection AddApplicationValidation(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("bs");
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        return services;
    }
}
