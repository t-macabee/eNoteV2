using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace eNote.Tests.TestUtils;

public sealed class InMemoryRowVersionModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
{
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetDeclaredProperties())
            {
                if (property.IsConcurrencyToken
                    && property.ValueGenerated == ValueGenerated.OnAddOrUpdate
                    && property.ClrType == typeof(byte[]))
                {
                    modelBuilder.Entity(entityType.Name)
                        .Property(property.Name)
                        .HasValueGenerator<InMemoryRowVersionGenerator>();
                }
            }
        }
    }

    private sealed class InMemoryRowVersionGenerator : ValueGenerator<byte[]>
    {
        public override byte[] Next(EntityEntry entry) => Guid.NewGuid().ToByteArray()[..8];

        public override bool GeneratesTemporaryValues => false;
    }
}
