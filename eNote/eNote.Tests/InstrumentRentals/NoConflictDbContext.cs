using eNote.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace eNote.Tests.InstrumentRentals;

internal sealed class NoConflictDbContext : IAppDbContext
{
    public DbSet<TEntity> Set<TEntity>() where TEntity : class =>
        throw new NotSupportedException("This stub does not support queries.");

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}