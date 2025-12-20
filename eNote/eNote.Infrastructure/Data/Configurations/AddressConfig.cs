using eNote.Domain.Entities.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations
{
    public sealed class AddressConfig : IEntityTypeConfiguration<Address>
    {
        public void Configure(EntityTypeBuilder<Address> builder)
        {
            builder.Property(a => a.City).IsRequired().HasMaxLength(100);
            builder.Property(a => a.Street).IsRequired().HasMaxLength(100);
            builder.Property(a => a.Number).IsRequired().HasMaxLength(20);
        }
    }
}
