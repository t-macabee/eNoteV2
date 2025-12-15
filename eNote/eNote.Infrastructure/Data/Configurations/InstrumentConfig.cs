using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations
{
    public sealed class InstrumentConfig : IEntityTypeConfiguration<Instrument>
    {
        public void Configure(EntityTypeBuilder<Instrument> builder)
        {
            builder.HasOne(x => x.MusicShop)
                   .WithMany()
                   .HasForeignKey(x => x.MusicShopId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.InstrumentType)
                   .WithMany(t => t.Instruments)
                   .HasForeignKey(x => x.InstrumentTypeId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(i => i.Model).IsRequired().HasMaxLength(100);
            builder.Property(i => i.Manufacturer).IsRequired().HasMaxLength(100);
            builder.Property(i => i.Description).HasMaxLength(1000);
        }
    }
}
