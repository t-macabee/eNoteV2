using eNote.Application.Constants;
using eNote.Domain.Entities.Rentals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class RentalPaymentConfig : IEntityTypeConfiguration<RentalPayment>
{
    public void Configure(EntityTypeBuilder<RentalPayment> builder)
    {
        builder.HasOne(x => x.InstrumentRental)
               .WithMany(r => r.Payments)
               .HasForeignKey(x => x.InstrumentRentalId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.MusicStoreId).IsRequired();

        builder.Property(x => x.StripePaymentIntentId)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasIndex(x => x.StripePaymentIntentId)
               .IsUnique()
               .HasDatabaseName(DbConstraintNames.RentalPaymentStripePaymentIntentIdUniqueIndex);

        builder.Property(x => x.StripeChargeId)
               .HasMaxLength(64);

        builder.Property(x => x.AmountChargedCents)
               .IsRequired();

        builder.Property(x => x.Currency)
               .HasMaxLength(3)
               .IsRequired();

        builder.Property(x => x.Status)
               .HasConversion<int>()
               .IsRequired();

        builder.Property(x => x.StripeEventId)
               .HasMaxLength(64);

        builder.HasIndex(x => x.StripeEventId)
               .IsUnique()
               .HasFilter("\"StripeEventId\" IS NOT NULL")
               .HasDatabaseName(DbConstraintNames.RentalPaymentStripeEventIdUniqueIndex);

        builder.Property(x => x.StripeRefundId)
               .HasMaxLength(64);

        builder.HasIndex(x => new { x.InstrumentRentalId, x.Status });
    }
}
