using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class InstrumentConfig : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.HasOne(x => x.MusicStore)
               .WithMany(x => x.Instruments)
               .HasForeignKey(x => x.MusicStoreId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InstrumentType)
               .WithMany(t => t.Instruments)
               .HasForeignKey(x => x.InstrumentTypeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Model).HasStringConfig(Instrument.ModelMaxLength, true);
        builder.Property(x => x.Manufacturer).HasStringConfig(Instrument.ManufacturerMaxLength, true);
        builder.Property(x => x.Description).HasMaxLength(Instrument.DescriptionMaxLength);

        builder.Ignore(x => x.IsAvailable);
    }
}
