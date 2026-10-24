using eNote.Application.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class PaymentRefundConfig : IEntityTypeConfiguration<PaymentRefund>
{
    public void Configure(EntityTypeBuilder<PaymentRefund> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StripeRefundId)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasIndex(x => x.StripeRefundId)
               .IsUnique()
               .HasDatabaseName(DbConstraintNames.PaymentRefundStripeRefundIdUniqueIndex);

        builder.Property(x => x.AmountCents)
               .IsRequired();

        builder.Property(x => x.AppliedAtUtc)
               .IsRequired();

        builder.HasOne(x => x.RentalPayment)
               .WithMany(p => p.Refunds)
               .HasForeignKey(x => x.RentalPaymentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CoursePayment)
               .WithMany(p => p.Refunds)
               .HasForeignKey(x => x.CoursePaymentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(t => t.HasCheckConstraint(
            DbConstraintNames.PaymentRefundExactlyOnePaymentCheck,
            """("RentalPaymentId" IS NOT NULL AND "CoursePaymentId" IS NULL) OR ("RentalPaymentId" IS NULL AND "CoursePaymentId" IS NOT NULL)"""));
    }
}
