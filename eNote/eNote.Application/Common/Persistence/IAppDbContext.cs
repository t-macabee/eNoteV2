using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Common.Persistence
{
    public interface IAppDbContext
    {
        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
