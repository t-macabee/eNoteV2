using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Common.Persistence;

public static class DbErrors
{
    public static bool IsUniqueViolation(DbUpdateException exception, string constraintName) =>
        exception.InnerException?.Message.Contains(constraintName, StringComparison.Ordinal) == true;
}
