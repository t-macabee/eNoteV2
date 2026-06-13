using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;

namespace eNote.API.Extensions
{
    public static class MiddlewareExtensions
    {
        public static WebApplication UseErrorHandling(this WebApplication app)
        {
            _ = app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.ContentType = "application/json";

                    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                    var (statusCode, errorCode, message) = exception switch
                    {
                        AppException appEx => (appEx.StatusCode, appEx.ErrorCode, appEx.Message),
                        ArgumentException => (400, "error.bad_request", exception?.Message ?? Messages.BadRequest),
                        _ => (500, "error.internal", Messages.InternalError)
                    };

                    context.Response.StatusCode = statusCode;

                    var logger = app.Services.GetService<ILogger<WebApplication>>();
                    logger?.LogError(exception, "Unhandled exception caught by middleware");

                    var response = new ErrorResponse
                    {
                        Status = statusCode,
                        Code = errorCode,
                        Message = message
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                });
            });

            return app;
        }

        private record ErrorResponse
        {
            public int Status { get; init; }
            public string Code { get; init; } = string.Empty;
            public string Message { get; init; } = string.Empty;
        }
    }
}
