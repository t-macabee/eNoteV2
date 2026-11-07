using eNote.Application.Constants;
using eNote.Application.Validation.Identity;
using eNote.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class AppUserConfig : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.FirstName)
            .HasMaxLength(IdentityPolicy.PersonNameMaxLength)
            .IsRequired();
        builder.Property(u => u.LastName)
            .HasMaxLength(IdentityPolicy.PersonNameMaxLength)
            .IsRequired();
        builder.Property(u => u.PicturePath).HasMaxLength(512);
        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName(DbConstraintNames.UserEmailUniqueIndex)
            .HasFilter("[NormalizedEmail] IS NOT NULL");
    }
}
