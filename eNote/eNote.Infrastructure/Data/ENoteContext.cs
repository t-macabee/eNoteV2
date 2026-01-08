using eNote.Application.Interfaces.Ports;
using eNote.Domain.Entities;
using eNote.Domain.Entities.Users;
using eNote.Infrastructure.Data.Seed;
using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data
{
    public class ENoteContext(DbContextOptions<ENoteContext> options) : IdentityDbContext<AppUser, AppRole, int>(options), IAppDbContext
    {
        public new DbSet<TEntity> Set<TEntity>() where TEntity : class => base.Set<TEntity>();

        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<Assignment> Assignments => Set<Assignment>();
        public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<Instructor> Instructors => Set<Instructor>();
        public DbSet<Instrument> Instruments => Set<Instrument>();
        public DbSet<InstrumentRental> InstrumentRentals => Set<InstrumentRental>();
        public DbSet<InstrumentType> InstrumentTypes => Set<InstrumentType>();
        public DbSet<Lecture> Lectures => Set<Lecture>();
        public DbSet<LectureNote> LectureNotes => Set<LectureNote>();
        public DbSet<MusicShop> MusicShops => Set<MusicShop>();        
        public DbSet<Student> Students => Set<Student>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ENoteContext).Assembly);

            ModelBuilderSeed.Seed(modelBuilder);
        }        
    }
}
