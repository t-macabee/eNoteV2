using eNote.Application.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class ManufacturerConfig : IEntityTypeConfiguration<Manufacturer>
{
    public void Configure(EntityTypeBuilder<Manufacturer> builder)
    {
        builder.Property(x => x.Name).HasStringConfig(Manufacturer.NameMaxLength, true);
        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName(DbConstraintNames.ManufacturerNameUniqueIndex);
    }
}
