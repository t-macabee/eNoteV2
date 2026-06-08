using eNote.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;

namespace eNote.API.Extensions
{
    public static class MiddlewareExtensions
    {
        public static WebApplication UseErrorHandling(this WebApplication app)
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.ContentType = "application/json";

                    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                    context.Response.StatusCode = exception switch
                    {
                        NotFoundException => StatusCodes.Status404NotFound,
                        ConflictException => StatusCodes.Status409Conflict,
                        BusinessException => StatusCodes.Status400BadRequest,
                        AuthorizationException => StatusCodes.Status403Forbidden,
                        AuthenticationException => StatusCodes.Status401Unauthorized,
                        ArgumentException => StatusCodes.Status400BadRequest,
                        _ => StatusCodes.Status500InternalServerError
                    };

                    var env = app.Environment.EnvironmentName;
                    var errorCode = (exception is IHasErrorCode coded) ? coded.Code : "error.internal";
                    var message = env == "Development" ? exception?.Message : (exception is IHasErrorCode ? GetDefaultMessageForCode(errorCode) : "Došlo je do greške na serveru.");

                    var response = new
                    {
                        status = context.Response.StatusCode,
                        code = errorCode,
                        message
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                });
            });

            return app;
        }

        private static string GetDefaultMessageForCode(string code)
        {
            return code switch
            {
                NotFoundException.DefaultCode => NotFoundException.DefaultMessage,
                BusinessException.DefaultCode => BusinessException.DefaultMessage,
                ConflictException.DefaultCode => ConflictException.DefaultMessage,
                AuthorizationException.DefaultCode => AuthorizationException.DefaultMessage,
                AuthenticationException.DefaultCode => AuthenticationException.DefaultMessage,
                _ => "Došlo je do greške na serveru."
            };
        }
    }
}
