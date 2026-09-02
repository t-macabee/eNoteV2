using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class MusicStoreConfig : IEntityTypeConfiguration<MusicStore>
{
    public void Configure(EntityTypeBuilder<MusicStore> builder)
    {
        builder.Property(m => m.StoreName).HasStringConfig(100, true);
        builder.Property(m => m.BusinessHours).HasStringConfig(50, true);
        builder.Property(m => m.PhoneNumber).HasMaxLength(30);
        builder.Property(m => m.ImagePath).HasMaxLength(500);

        builder.HasOne(x => x.Address)
            .WithMany()
            .HasForeignKey(x => x.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Employees)
               .WithOne(e => e.MusicStore)
               .HasForeignKey(e => e.MusicStoreId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Instruments)
               .WithOne(i => i.MusicStore)
               .HasForeignKey(i => i.MusicStoreId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AddressId);
    }
}
