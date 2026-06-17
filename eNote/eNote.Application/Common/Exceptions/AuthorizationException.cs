namespace eNote.Application.Common.Exceptions;

public class AuthorizationException(string? message = null) : AppException(403, "error.forbidden", message)
{
}
