using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class AddressConfig : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.HasOne(a => a.City)
               .WithMany(c => c.Addresses)
               .HasForeignKey(a => a.CityId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.Property(a => a.Street).HasStringConfig(100, true);
        builder.Property(a => a.Number).HasStringConfig(20, true);
    }
}
