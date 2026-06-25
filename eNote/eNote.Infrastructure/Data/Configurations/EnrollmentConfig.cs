using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class EnrollmentConfig : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasQueryFilter(e => e.Course.IsActive);

        builder.HasOne(x => x.Student)
               .WithMany(s => s.Enrollments)
               .HasForeignKey(x => x.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Course)
               .WithMany(x => x.Enrollments)
               .HasForeignKey(x => x.CourseId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
        builder.Property(e => e.EnrollmentStatus).HasConversion<int>();
    }
}
