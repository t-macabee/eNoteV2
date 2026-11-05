using eNote.Application.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class InstrumentTypeConfig : IEntityTypeConfiguration<InstrumentType>
{
    public void Configure(EntityTypeBuilder<InstrumentType> builder)
    {
        builder.Property(t => t.Type).HasStringConfig(InstrumentType.TypeMaxLength, true);
        builder.Property(t => t.MonthlyFee).HasDecimalPrecision();
        builder.HasIndex(t => t.Type).IsUnique().HasDatabaseName(DbConstraintNames.InstrumentTypeTypeUniqueIndex);
    }
}
