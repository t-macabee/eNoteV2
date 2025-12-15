using eNote.Domain.Entities;
using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data.Context
{
    public class ENoteContext(DbContextOptions<ENoteContext> options) : IdentityDbContext<AppUser, AppRole, int>(options)
    {
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Instructor> Instructors => Set<Instructor>();
        public DbSet<MusicShop> MusicShops => Set<MusicShop>();
        public DbSet<Course> Courses => Set<Course>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ENoteContext).Assembly);
        }
    }
}
