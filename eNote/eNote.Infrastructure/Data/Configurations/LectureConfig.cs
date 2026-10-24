using eNote.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class LectureConfig : IEntityTypeConfiguration<Lecture>
{
    public void Configure(EntityTypeBuilder<Lecture> builder)
    {
        builder.HasOne(p => p.Course)
               .WithMany(k => k.Lectures)
               .HasForeignKey(p => p.CourseId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
               .WithMany()
               .HasForeignKey(p => p.CancelledById)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Student)
               .WithMany()
               .HasForeignKey(p => p.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.Name).HasStringConfig(200, true);
        builder.Property(p => p.Location).HasStringConfig(200, true);
        builder.Property(p => p.Duration).IsRequired();
        builder.Property(p => p.LectureType).HasConversion<int>();
        builder.Property(p => p.LectureStatus).HasConversion<int>();
        builder.Property(p => p.LectureTime).IsRequired();
        builder.Property(p => p.Capacity).IsRequired(false);
        builder.Property(p => p.CancellationReason).HasMaxLength(500);
        builder.Ignore(p => p.IsCancelled);
        builder.Ignore(p => p.IsIndividual);
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.HasQueryFilter(p => p.IsActive);
        builder.Property(p => p.Version).IsRowVersion();
    }
}
