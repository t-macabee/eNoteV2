using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class AttendanceConfig : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.HasQueryFilter(a => a.Lecture.IsActive);

        builder.HasOne(p => p.Student)
               .WithMany(s => s.Attendances)
               .HasForeignKey(p => p.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Lecture)
               .WithMany(p => p.Attendances)
               .HasForeignKey(p => p.LectureId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.StudentId, p.LectureId }).IsUnique();
        builder.Property(p => p.AttendanceStatus).HasConversion<int>();
    }
}
