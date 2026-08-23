using eNote.Contracts.Communication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class RentalNotificationOutboxConfig : IEntityTypeConfiguration<RentalNotificationOutbox>
{
    public void Configure(EntityTypeBuilder<RentalNotificationOutbox> builder)
    {
        builder.ToTable("RentalNotificationOutbox");

        // Explicit DB-level default (not just the Domain entity's in-memory default) so the
        // migration backfills pre-existing rows with the correct discriminator instead of "" —
        // an empty MessageType would make RentalNotificationOutboxPublisher throw for any row
        // queued before this column existed.
        builder.Property(x => x.MessageType)
            .HasMaxLength(64)
            .HasDefaultValue(NotificationMessageTypes.RentalStatusChanged)
            .IsRequired();

        builder.Property(x => x.PayloadJson)
            .IsRequired();

        builder.Property(x => x.LastError)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.PublishedAt);
    }
}