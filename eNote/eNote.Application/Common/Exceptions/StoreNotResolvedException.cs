namespace eNote.Application.Common.Exceptions;

public sealed class StoreNotResolvedException(string? message = null) : BusinessException(message)
{
}
