namespace eNote.Application.Common.Exceptions;

public class NotFoundException(string? message = null) : AppException(404, "error.not_found", message) { }