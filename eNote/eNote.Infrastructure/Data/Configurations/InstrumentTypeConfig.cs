using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations
{
    public sealed class InstrumentTypeConfig : IEntityTypeConfiguration<InstrumentType>
    {
        public void Configure(EntityTypeBuilder<InstrumentType> builder)
        {
            builder.Property(t => t.Type)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(t => t.MonthlyFee)
                   .IsRequired()
                   .HasPrecision(18, 2);
        }
    }
}
