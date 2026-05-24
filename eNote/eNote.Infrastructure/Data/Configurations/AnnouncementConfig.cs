using eNote.Domain.Entities;
using eNote.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations
{
    public sealed class AnnouncementConfig : IEntityTypeConfiguration<Announcement>
    {
        public void Configure(EntityTypeBuilder<Announcement> builder)
        {
            builder.Property(x => x.Title).IsRequired().HasMaxLength(150);
            builder.Property(x => x.Content).IsRequired().HasMaxLength(4000);

            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.MusicStore)
                .WithMany()
                .HasForeignKey(x => x.MusicStoreId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.PublishedAt);
            builder.HasIndex(x => x.CourseId);
            builder.HasIndex(x => x.MusicStoreId);

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_Announcement_Scope",
                "([CourseId] IS NOT NULL AND [MusicStoreId] IS NULL) OR ([CourseId] IS NULL AND [MusicStoreId] IS NOT NULL)"));
        }
    }
}
