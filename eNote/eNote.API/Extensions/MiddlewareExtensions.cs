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
                        KeyNotFoundException => StatusCodes.Status404NotFound,
                        UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                        ArgumentException => StatusCodes.Status400BadRequest,
                        _ => StatusCodes.Status500InternalServerError
                    };

                    var response = new
                    {
                        error = app.Environment.IsDevelopment() ? exception?.Message : "Došlo je do greške na serveru."
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                });
            });

            return app;
        }
    }
}
