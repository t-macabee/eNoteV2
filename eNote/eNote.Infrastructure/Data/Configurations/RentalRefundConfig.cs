using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class RentalRefundConfig : IEntityTypeConfiguration<RentalRefund>
{
    public void Configure(EntityTypeBuilder<RentalRefund> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StripeRefundId)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasIndex(x => x.StripeRefundId)
               .IsUnique();

        builder.Property(x => x.AmountCents)
               .IsRequired();

        builder.Property(x => x.AppliedAtUtc)
               .IsRequired();

        builder.HasOne(x => x.RentalPayment)
               .WithMany(p => p.Refunds)
               .HasForeignKey(x => x.RentalPaymentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
