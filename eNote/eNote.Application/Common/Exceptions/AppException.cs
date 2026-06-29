using eNote.Application.Common.Localization;

namespace eNote.Application.Common.Exceptions;

public abstract class AppException(int statusCode, string errorCode, string? message = null) : Exception(message ?? GetDefaultMessage(statusCode))
{
    public int StatusCode { get; } = statusCode;
    public string ErrorCode { get; } = errorCode;

    private static string GetDefaultMessage(int statusCode) => statusCode switch
    {
        400 => Messages.BadRequest,
        401 => Messages.Unauthorized,
        403 => Messages.Forbidden,
        404 => Messages.NotFound,
        409 => Messages.Conflict,
        _ => Messages.InternalError
    };
}
