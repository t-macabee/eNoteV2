using eNote.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class InstrumentViewConfig : IEntityTypeConfiguration<InstrumentView>
{
    public void Configure(EntityTypeBuilder<InstrumentView> builder)
    {
        builder.HasOne<AppUser>()
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Instrument>()
               .WithMany()
               .HasForeignKey(x => x.InstrumentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.InstrumentId }).IsUnique();
        builder.HasIndex(x => x.LastViewedAt);
    }
}
