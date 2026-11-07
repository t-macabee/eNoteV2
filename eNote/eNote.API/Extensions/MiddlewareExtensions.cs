using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using Microsoft.AspNetCore.Diagnostics;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace eNote.API.Extensions;

public static class MiddlewareExtensions
{
    private static readonly JsonSerializerOptions ErrorJsonOptions = new(JsonSerializerDefaults.Web);

    public static WebApplication UseErrorHandling(this WebApplication app)
    {
        _ = app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            SuppressDiagnosticsCallback = context => context.Exception is AppException,
            ExceptionHandler = async context =>
            {
                context.Response.ContentType = "application/json";

                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                (var statusCode, var errorCode, var message) = exception switch
                {
                    AppException appEx => (appEx.StatusCode, appEx.ErrorCode, appEx.Message),
                    _ => (500, "error.internal", Messages.InternalError)
                };

                context.Response.StatusCode = statusCode;

                var logger = context.RequestServices.GetService<ILogger<WebApplication>>();
                var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
                var userId = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                var method = context.Request.Method;
                var path = context.Request.Path.Value;

                if (statusCode >= StatusCodes.Status500InternalServerError)
                {
                    logger?.LogError(exception, "Unhandled exception caught by middleware. {Method} {Path} {UserId} {TraceId}", method, path, userId, traceId);
                }
                else
                {
                    logger?.LogWarning(exception, "Request failed with {StatusCode} {ErrorCode}: {Message}. {Method} {Path} {UserId} {TraceId}", statusCode, errorCode, message, method, path, userId, traceId);
                }

                var response = new ErrorResponse
                {
                    Status = statusCode,
                    Code = errorCode,
                    Message = message,
                    TraceId = traceId
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response, ErrorJsonOptions), context.RequestAborted);
            }
        });

        return app;
    }
}
