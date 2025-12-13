using eNote.Infrastructure.Data.Entities;
using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data
{
    public class ENoteContext(DbContextOptions<ENoteContext> options) : IdentityDbContext<AppUser, AppRole, int>(options)
    {
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Instructor> Instructors => Set<Instructor>();
        public DbSet<MusicShop> MusicShops => Set<MusicShop>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AppUser>()
                .HasOne(u => u.Address)
                .WithMany()
                .HasForeignKey(u => u.AddressId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Student>(entity =>
            {
                entity.HasOne(s => s.AppUser)
                      .WithOne()
                      .HasForeignKey<Student>(s => s.AppUserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(s => s.AppUserId)
                      .IsUnique();
            });

            builder.Entity<Instructor>(entity =>
            {
                entity.HasOne(i => i.AppUser)
                      .WithOne()
                      .HasForeignKey<Instructor>(i => i.AppUserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(i => i.AppUserId)
                      .IsUnique();
            });

            builder.Entity<MusicShop>(entity =>
            {
                entity.HasOne(m => m.AppUser)
                      .WithOne()
                      .HasForeignKey<MusicShop>(m => m.AppUserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(m => m.AppUserId)
                      .IsUnique();
            });
        }
    }
}
