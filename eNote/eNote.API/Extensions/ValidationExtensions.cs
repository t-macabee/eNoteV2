using Microsoft.AspNetCore.Http.Features;
using eNote.API.Filters;
using eNote.Application.Common.Localization;
using eNote.Application.Validation;
using eNote.Application.Validation.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;

namespace eNote.API.Extensions;

public static class ValidationExtensions
{
    public static IServiceCollection AddApplicationValidation(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("bs");
        ValidatorOptions.Global.DisplayNameResolver = ValidationDisplayNames.Resolve;
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
        services.Configure<MvcOptions>(options => options.Filters.Add<FluentValidationFilter>(FluentValidationFilter.FilterOrder));

        services.PostConfigure<ApiBehaviorOptions>(options =>
        {
            options.SuppressMapClientErrors = true;
            options.InvalidModelStateResponseFactory = context =>
            {
                var request = context.HttpContext.Request;
                var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

                if (request.ContentLength > context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>()?.MaxRequestBodySize)
                {
                    return new ObjectResult(new ErrorResponse
                    {
                        Status = StatusCodes.Status413PayloadTooLarge,
                        Code = "error.file_too_large",
                        Message = Messages.FileTooLarge,
                        TraceId = traceId
                    })
                    {
                        StatusCode = StatusCodes.Status413PayloadTooLarge
                    };
                }

                if (request.HasFormContentType && context.ModelState.ContainsKey(string.Empty))
                {
                    return new BadRequestObjectResult(new ErrorResponse
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Code = "error.validation",
                        Message = Messages.FileNotReadable,
                        TraceId = traceId
                    });
                }

                var errors = context.ModelState
                    .Where(entry => entry.Value is { Errors.Count: > 0 })
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors
                            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? Messages.BadRequest : error.ErrorMessage)
                            .ToArray());

                return new BadRequestObjectResult(new ErrorResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Code = "error.validation",
                    Message = string.Join("\n", errors.Values.SelectMany(messages => messages)),
                    Errors = errors,
                    TraceId = traceId
                });
            };
        });

        return services;
    }
}
