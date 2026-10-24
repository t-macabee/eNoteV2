namespace eNote.Application.Common.Exceptions;

public sealed class CurrentPasswordInvalidException(string? message = null)
    : AppException(400, "error.current_password_invalid", message ?? Messages.CurrentPasswordIncorrect)
{
}
