using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations
{
    public sealed class LectureConfig : IEntityTypeConfiguration<Lecture>
    {
        public void Configure(EntityTypeBuilder<Lecture> builder)
        {
            builder.HasOne(p => p.Course)
                   .WithMany(k => k.Lectures)
                   .HasForeignKey(p => p.CourseId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Location).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Duration).IsRequired();
            builder.Property(p => p.LectureType).HasConversion<int>();
            builder.Property(p => p.LectureStatus).HasConversion<int>();
            builder.Property(p => p.LectureTime).IsRequired();
        }
    }
}
