using eNote.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class EventConfig : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.Property(x => x.Title).HasStringConfig(150, true);
        builder.Property(x => x.Description).HasStringConfig(4000, true);
        builder.Property(x => x.StartsAt).IsRequired();
        builder.Property(x => x.EndsAt).IsRequired(false);

        builder.HasOne(x => x.Address)
            .WithMany()
            .HasForeignKey(x => x.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Instructor)
            .WithMany()
            .HasForeignKey(x => x.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.StartsAt);
        builder.HasIndex(x => x.CourseId);
        builder.HasIndex(x => x.InstructorId);
        builder.HasIndex(x => x.AddressId);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Event_EndsAfterStarts",
            "\"EndsAt\" IS NULL OR \"EndsAt\" > \"StartsAt\""));
    }
}
