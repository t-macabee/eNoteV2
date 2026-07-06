using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class AddressConfig : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.Property(a => a.City).HasStringConfig(100, true);
        builder.Property(a => a.Street).HasStringConfig(100, true);
        builder.Property(a => a.Number).HasStringConfig(20, true);
    }
}
