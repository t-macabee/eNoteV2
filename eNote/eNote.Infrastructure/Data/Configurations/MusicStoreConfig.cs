using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations
{
    public sealed class MusicStoreConfig : IEntityTypeConfiguration<MusicStore>
    {
        public void Configure(EntityTypeBuilder<MusicStore> builder)
        {
            builder.Property(m => m.StoreName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(m => m.BusinessHours)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasMany(x => x.Employees)
                   .WithOne(e => e.MusicStore)
                   .HasForeignKey(e => e.MusicStoreId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Instruments)
                   .WithOne(i => i.MusicStore)
                   .HasForeignKey(i => i.MusicStoreId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
