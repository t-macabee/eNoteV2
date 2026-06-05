using eNote.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace eNote.API.Filters
{
    public sealed class HttpResponseExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            var status = exception switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                ConflictException => StatusCodes.Status409Conflict,
                BusinessException => StatusCodes.Status400BadRequest,
                AuthorizationException => StatusCodes.Status403Forbidden,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                ArgumentException => StatusCodes.Status400BadRequest,
                InvalidOperationException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            var response = new
            {
                error = context.HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.EnvironmentName == "Development"
                    ? exception.Message
                    : "Došlo je do greške na serveru."
            };

            context.Result = new ObjectResult(response) { StatusCode = status };
            context.ExceptionHandled = true;
        }
    }
}
