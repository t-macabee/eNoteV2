using eNote.Domain.Entities.Rentals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class InstrumentTypeConfig : IEntityTypeConfiguration<InstrumentType>
{
    public void Configure(EntityTypeBuilder<InstrumentType> builder)
    {
        builder.Property(t => t.Type).HasStringConfig(100, true);
        builder.Property(t => t.MonthlyFee).HasDecimalPrecision(18, 2);
    }
}
