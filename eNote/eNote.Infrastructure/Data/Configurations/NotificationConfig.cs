using eNote.Application.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class NotificationConfig : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.IsRead).HasDefaultValue(false);

        builder.HasIndex(x => new { x.UserId, x.IsRead });
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.UserId, x.RentalId, x.Title });
        builder.HasIndex(x => new { x.UserId, x.RentalId, x.CreatedAt }).IsUnique();

        builder.HasIndex(x => new { x.UserId, x.LectureId, x.CreatedAt })
            .HasDatabaseName(DbConstraintNames.NotificationUserLectureCreatedAtUniqueIndex)
            .IsUnique();
        builder.HasIndex(x => new { x.UserId, x.SubmissionId, x.CreatedAt })
            .HasDatabaseName(DbConstraintNames.NotificationUserSubmissionCreatedAtUniqueIndex)
            .IsUnique();
    }
}
