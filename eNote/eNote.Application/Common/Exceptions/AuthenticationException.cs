namespace eNote.Application.Common.Exceptions;

public class AuthenticationException(string? message = null) : AppException(401, "error.unauthorized", message)
{
}
