using eNote.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace eNote.Tests.TestUtils;

public sealed class ThrowingSaveDbContext(IAppDbContext inner, Exception exception, Func<Task>? beforeThrow = null) : IAppDbContext
{
    public DbSet<TEntity> Set<TEntity>() where TEntity : class => inner.Set<TEntity>();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => ThrowAsync();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        inner.BeginTransactionAsync(cancellationToken);

    private async Task<int> ThrowAsync()
    {
        if (beforeThrow is not null)
        {
            await beforeThrow();
        }

        throw exception;
    }
}
