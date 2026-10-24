using eNote.Application.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class CoursePaymentConfig : IEntityTypeConfiguration<CoursePayment>
{
    public void Configure(EntityTypeBuilder<CoursePayment> builder)
    {
        builder.HasOne(x => x.Enrollment)
               .WithMany(e => e.Payments)
               .HasForeignKey(x => x.EnrollmentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.StripePaymentIntentId)
               .HasMaxLength(64)
               .IsRequired();

        builder.HasIndex(x => x.StripePaymentIntentId)
               .IsUnique()
               .HasDatabaseName(DbConstraintNames.CoursePaymentPaymentIntentIdUniqueIndex);

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
               .HasDatabaseName(DbConstraintNames.CoursePaymentStripeEventIdUniqueIndex);

        builder.Property(x => x.StripeRefundId)
               .HasMaxLength(64);

        builder.HasIndex(x => new { x.EnrollmentId, x.Status });

        builder.HasIndex(x => x.EnrollmentId)
               .IsUnique()
               .HasFilter("\"Status\" IN (1, 3)")
               .HasDatabaseName(DbConstraintNames.CoursePaymentOpenAttemptUniqueIndex);

        builder.Property(x => x.Version).IsRowVersion();
    }
}
