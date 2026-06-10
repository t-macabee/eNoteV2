namespace eNote.Application.Common.Exceptions;

public class ConflictException(string? message = null) : AppException(409, "error.conflict", message)
{  
}