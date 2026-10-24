using eNote.Application.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class MembershipPaymentConfig : IEntityTypeConfiguration<MembershipPayment>
{
    public void Configure(EntityTypeBuilder<MembershipPayment> builder)
    {
        builder.HasOne(x => x.Student)
               .WithMany()
               .HasForeignKey(x => x.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Amount).HasDecimalPrecision(10).IsRequired();
        builder.Property(x => x.ReceiptNumber).HasStringConfig(maxLength: 50, isRequired: true);

        builder.HasIndex(x => x.ReceiptNumber)
               .IsUnique()
               .HasDatabaseName(DbConstraintNames.MembershipPaymentReceiptNumberUniqueIndex);

        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasIndex(x => new { x.StudentId, x.PaidAt });
    }
}
