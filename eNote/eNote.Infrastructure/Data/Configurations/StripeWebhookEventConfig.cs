using eNote.Application.Constants;
using eNote.Domain.Entities.Rentals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class StripeWebhookEventConfig : IEntityTypeConfiguration<StripeWebhookEvent>
{
    public void Configure(EntityTypeBuilder<StripeWebhookEvent> builder)
    {
        builder.Property(x => x.StripeEventId)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasIndex(x => x.StripeEventId)
               .IsUnique()
               .HasDatabaseName(DbConstraintNames.StripeWebhookEventStripeEventIdUniqueIndex);

        builder.Property(x => x.Type)
               .HasMaxLength(64)
               .IsRequired();

        builder.Property(x => x.PayloadJson)
               .IsRequired();

        builder.Property(x => x.ProcessedAt)
               .IsRequired();
    }
}
