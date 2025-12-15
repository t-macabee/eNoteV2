using eNote.Domain.Entities;
using eNote.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations
{
    public sealed class InstructorConfig : IEntityTypeConfiguration<Instructor>
    {
        public void Configure(EntityTypeBuilder<Instructor> builder)
        {
            builder.HasOne<AppUser>()
                .WithOne()
                .HasForeignKey<Instructor>(i => i.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(i => i.AppUserId).IsUnique();
        }
    }
}
