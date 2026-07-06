using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class RentalNotificationOutboxConfig : IEntityTypeConfiguration<RentalNotificationOutbox>
{
    public void Configure(EntityTypeBuilder<RentalNotificationOutbox> builder)
    {
        builder.ToTable("RentalNotificationOutbox");

        builder.Property(x => x.PayloadJson)
            .IsRequired();

        builder.Property(x => x.LastError)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.PublishedAt);
    }
}