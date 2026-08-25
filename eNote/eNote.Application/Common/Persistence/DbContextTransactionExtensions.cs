using Microsoft.EntityFrameworkCore.Storage;

namespace eNote.Application.Common.Persistence;

public static class DbContextTransactionExtensions
{
    public static async Task<T> ExecuteInTransactionAsync<T>(
        this IAppDbContext context,
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        using IDbContextTransaction transaction = await context.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await action();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public static async Task ExecuteInTransactionAsync(
        this IAppDbContext context,
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        using IDbContextTransaction transaction = await context.BeginTransactionAsync(cancellationToken);

        try
        {
            await action();
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
