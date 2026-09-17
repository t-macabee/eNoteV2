using eNote.Application.Constants;
using eNote.Domain.Entities.Communication;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace eNote.Tests.Data;

public sealed class NotificationConfigTests
{
    [Fact]
    public void RentalUniqueIndex_HasExplicitDatabaseName()
    {
        using var context = TestDbContextFactory.CreateContext(new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));

        var entity = context.Model.FindEntityType(typeof(Notification));
        Assert.NotNull(entity);
        var index = entity.GetIndexes().Single(i =>
            i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(["UserId", "RentalId", "CreatedAt"]));

        Assert.Equal(DbConstraintNames.NotificationUserRentalCreatedAtUniqueIndex, index.GetDatabaseName());
        Assert.Equal(
            DbConstraintNames.NotificationUserRentalCreatedAtUniqueIndex,
            index.FindAnnotation(RelationalAnnotationNames.Name)?.Value);
    }
}
