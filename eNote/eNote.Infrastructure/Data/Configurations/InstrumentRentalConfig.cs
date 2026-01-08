using eNote.Domain.Entities;
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

            builder.Property(x => x.Price).HasColumnType("decimal(8,2)");
            builder.Property(x => x.RentalStatus).HasConversion<int>();                     
        }
    }
}
