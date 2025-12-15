using eNote.Domain.Entities;
using eNote.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations
{
    public sealed class MusicShopConfig : IEntityTypeConfiguration<MusicShop>
    {
        public void Configure(EntityTypeBuilder<MusicShop> builder)
        {
            builder.HasOne<AppUser>()
                .WithOne()
                .HasForeignKey<MusicShop>(m => m.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(m => m.AppUserId).IsUnique();
        }
    }
}
