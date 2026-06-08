using eNote.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace eNote.API.Filters
{
    public sealed class HttpResponseExceptionFilter(Microsoft.Extensions.Localization.IStringLocalizer<HttpResponseExceptionFilter>? localizer = null) : IExceptionFilter
    {
        private readonly Microsoft.Extensions.Localization.IStringLocalizer<HttpResponseExceptionFilter>? _localizer = localizer;

        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            var status = exception switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                ConflictException => StatusCodes.Status409Conflict,
                BusinessException => StatusCodes.Status400BadRequest,
                AuthorizationException => StatusCodes.Status403Forbidden,
                AuthenticationException => StatusCodes.Status401Unauthorized,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                ArgumentException => StatusCodes.Status400BadRequest,
                InvalidOperationException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            var env = context.HttpContext.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>()?.EnvironmentName;

            var errorCode = (exception is IHasErrorCode coded) ? coded.Code : "error.internal";

            string message;
            if (env == "Development")
            {
                message = exception.Message;
            }
            else if (_localizer != null)
            {
                var localized = _localizer[errorCode];
                message = localized.ResourceNotFound ? GetDefaultMessageForCode(errorCode) : localized.Value;
            }
            else
            {
                message = (exception is IHasErrorCode) ? GetDefaultMessageForCode(errorCode) : "Došlo je do greške na serveru.";
            }

            var response = new
            {
                status,
                code = errorCode,
                message
            };

            context.Result = new ObjectResult(response) { StatusCode = status };
            context.ExceptionHandled = true;
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
