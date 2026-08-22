using eNote.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace eNote.Tests.TestUtils;

public sealed class ThrowingSaveDbContext(IAppDbContext inner, Exception exception) : IAppDbContext
{
    public DbSet<TEntity> Set<TEntity>() where TEntity : class => inner.Set<TEntity>();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw exception;

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        inner.BeginTransactionAsync(cancellationToken);
}
