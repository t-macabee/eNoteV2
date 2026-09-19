using eNote.Contracts.Communication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class RentalNotificationOutboxConfig : IEntityTypeConfiguration<NotificationOutbox>
{
    public void Configure(EntityTypeBuilder<NotificationOutbox> builder)
    {
        builder.ToTable("RentalNotificationOutbox");

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