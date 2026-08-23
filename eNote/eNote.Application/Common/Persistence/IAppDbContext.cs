using Microsoft.EntityFrameworkCore.Storage;

namespace eNote.Application.Common.Persistence;

/// <summary>
/// Persistence seam over the EF Core DbContext. This is a DELIBERATE, accepted
/// tradeoff (the Jason-Taylor clean-architecture pattern), not a technology-agnostic
/// abstraction. It hides construction of the context, not the dependency on EF Core:
/// the exposed <see cref="DbSet{TEntity}"/> plus the ambient
/// `global using Microsoft.EntityFrameworkCore` (GlobalUsings.cs) mean query
/// composition (Include/ToListAsync/…) across the Application layer IS EF Core.
/// Consequences: swapping persistence tech is NOT an Infrastructure-only change,
/// and Application services are tested against a real in-memory EF provider
/// (TestDbContextFactory / EFCore.InMemory), not a hand-written double.
/// </summary>
public interface IAppDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
