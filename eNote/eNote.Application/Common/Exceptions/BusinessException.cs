namespace eNote.Application.Common.Exceptions;

public class BusinessException(string? message = null) : AppException(400, "error.business", message) 
{
}