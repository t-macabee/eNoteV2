using eNote.Domain.Entities;
using eNote.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public class MusicStoreEmployeeConfig : IEntityTypeConfiguration<MusicStoreEmployee>
{
    public void Configure(EntityTypeBuilder<MusicStoreEmployee> builder)
    {
        builder.HasUniqueIndex(nameof(MusicStoreEmployee.AppUserId));
        builder.HasUniqueIndex(nameof(MusicStoreEmployee.MusicStoreId), nameof(MusicStoreEmployee.AppUserId));

        builder.Property(x => x.IsManager).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasOne(x => x.MusicStore)
               .WithMany(x => x.Employees)
               .HasForeignKey(x => x.MusicStoreId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
               .WithMany()
               .HasForeignKey(x => x.AppUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
