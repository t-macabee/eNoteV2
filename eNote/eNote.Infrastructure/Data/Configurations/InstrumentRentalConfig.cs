using eNote.Application.Constants;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class InstrumentRentalConfig : IEntityTypeConfiguration<InstrumentRental>
{
    public void Configure(EntityTypeBuilder<InstrumentRental> builder)
    {
        builder.HasOne(x => x.StudentProfile)
               .WithMany(s => s.InstrumentRentals)
               .HasForeignKey(x => x.StudentProfileId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Instrument)
               .WithMany(x => x.InstrumentRentals)
               .HasForeignKey(x => x.InstrumentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.MusicStoreId).IsRequired();

        builder.Property(x => x.Fee).HasDecimalPrecision(10).IsRequired();

        builder.Property(x => x.IsPaid).HasDefaultFalse();

        builder.Property(x => x.PaidAt);

        builder.Property(x => x.AmountPaid).HasDecimalPrecision(10);

        builder.Property(x => x.RentalStatus)
               .HasConversion<int>();

        builder.HasIndex(x => x.InstrumentId)
               .HasFilter($"\"{nameof(InstrumentRental.RentalStatus)}\" IN ({(int)InstrumentRentalStatus.Approved}, {(int)InstrumentRentalStatus.Active})").IsUnique()
               .HasDatabaseName(DbConstraintNames.InstrumentRentalActiveOrApprovedUniqueIndex);
    }
}
