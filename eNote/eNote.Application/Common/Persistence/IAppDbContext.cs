using eNote.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Common.Persistence
{
    public interface IAppDbContext
    {
        DbSet<Student> Students { get; }
        DbSet<Instructor> Instructors { get; }
        DbSet<MusicShop> MusicShops { get; }

        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
