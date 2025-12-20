using eNote.Domain.Entities.Lectures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations
{
    public sealed class LectureNoteConfig : IEntityTypeConfiguration<LectureNote>
    {
        public void Configure(EntityTypeBuilder<LectureNote> builder)
        {
            builder.HasOne(n => n.Lecture)
                   .WithMany(p => p.LectureNotes)
                   .HasForeignKey(n => n.LectureId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
            builder.Property(n => n.Content).IsRequired();
            builder.Property(n => n.CreatedAt).IsRequired();
        }
    }
}
