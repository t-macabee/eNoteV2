using eNote.Domain.Entities.Rentals;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class InstrumentRentalConfig : IEntityTypeConfiguration<InstrumentRental>
{
    public void Configure(EntityTypeBuilder<InstrumentRental> builder)
    {
        // intentionally excludes rentals for inactive instruments globally; use IgnoreQueryFilters() for historical/audit queries
        builder.HasQueryFilter(r => r.Instrument.IsActive);

        builder.HasOne(x => x.StudentProfile)
               .WithMany(s => s.InstrumentRentals)
               .HasForeignKey(x => x.StudentProfileId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Instrument)
               .WithMany(x => x.InstrumentRentals)
               .HasForeignKey(x => x.InstrumentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.MusicStoreId).IsRequired();

        builder.Property(x => x.Fee).HasDecimalPrecision(10, 2).IsRequired();

        builder.Property(x => x.RentalStatus)
               .HasConversion<int>();

        builder.HasIndex(x => x.InstrumentId)
               .HasFilter(
                    $"\"{nameof(InstrumentRental.RentalStatus)}\" IN ({(int)InstrumentRentalStatus.Approved}, {(int)InstrumentRentalStatus.Active})"
               ).IsUnique();
    }
}
