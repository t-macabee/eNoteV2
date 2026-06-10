using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations
{
    public sealed class AssignmentConfig : IEntityTypeConfiguration<Assignment>
    {
        public void Configure(EntityTypeBuilder<Assignment> builder)
        {
            builder.HasOne(a => a.Lecture)
                   .WithMany(l => l.Assignments)
                   .HasForeignKey(a => a.LectureId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(a => a.Title).HasStringConfig(200, true);
            builder.Property(a => a.Description).IsRequired();
        }
    }
}
