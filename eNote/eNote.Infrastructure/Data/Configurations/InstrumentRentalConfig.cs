using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations
{
    public sealed class InstrumentRentalConfig : IEntityTypeConfiguration<InstrumentRental>
    {
        public void Configure(EntityTypeBuilder<InstrumentRental> builder)
        {
            builder.HasOne(x => x.Student)
                  .WithMany(s => s.InstrumentRentals)
                  .HasForeignKey(x => x.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Instrument)
                   .WithMany(x => x.InstrumentRentals)
                   .HasForeignKey(x => x.InstrumentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.Fee)
                   .HasPrecision(10, 2)  
                   .IsRequired();

            builder.Property(x => x.RentalStatus)
                .HasConversion<int>();

            builder.HasIndex(x => x.InstrumentId)
                .HasFilter(
                    $"[{nameof(InstrumentRental.RentalStatus)}] IN ({(int)InstrumentRentalStatus.Approved}, {(int)InstrumentRentalStatus.Active})"
                ).IsUnique();
        }
    }
}
