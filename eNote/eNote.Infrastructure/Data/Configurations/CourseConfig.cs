using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations
{
    public sealed class CourseConfig : IEntityTypeConfiguration<Course>
    {
        public void Configure(EntityTypeBuilder<Course> builder)
        {
            builder.HasOne(x => x.Instructor)
                   .WithMany(i => i.Courses)
                   .HasForeignKey(x => x.InstructorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Lectures)
                   .WithOne(x => x.Course)
                   .HasForeignKey(x => x.CourseId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.Price).HasColumnType("decimal(8,2)");
        }        
    }
}
