using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Common.Persistence
{
    public interface IAppDbContext
    {
        DbSet<Student> Students { get; }
        DbSet<Instructor> Instructors { get; }
        DbSet<MusicStore> MusicStores { get; }
        DbSet<MusicStoreEmployee> StoreEmployees { get; }

        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
