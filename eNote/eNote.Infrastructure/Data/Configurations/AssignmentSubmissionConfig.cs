using eNote.Domain.Entities.Lectures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations
{
    public sealed class AssignmentSubmissionConfig : IEntityTypeConfiguration<AssignmentSubmission>
    {
        public void Configure(EntityTypeBuilder<AssignmentSubmission> builder)
        {
            builder.HasOne(s => s.Assignment)
                   .WithMany(a => a.AssignmentSubmissions)
                   .HasForeignKey(s => s.AssignmentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.Student)
                   .WithMany(s => s.AssignmentSubmissions) 
                   .HasForeignKey(s => s.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);
            
            builder.Property(s => s.Grade).HasDefaultValue(null);
            builder.Property(s => s.FilePath).HasMaxLength(500);
        }
    }
}
