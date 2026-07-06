using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class InstrumentViewConfig : IEntityTypeConfiguration<InstrumentView>
{
    public void Configure(EntityTypeBuilder<InstrumentView> builder)
    {
        builder.HasIndex(x => new { x.UserId, x.InstrumentId }).IsUnique();
        builder.HasIndex(x => x.LastViewedAt);
    }
}
