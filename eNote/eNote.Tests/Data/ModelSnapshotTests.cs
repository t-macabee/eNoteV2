using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Data;

public sealed class ModelSnapshotTests
{
    [Fact]
    public void Model_HasNoChangesWithoutMigration()
    {
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseSqlServer(
                "Server=localhost;Database=model-snapshot-check;Trusted_Connection=True;TrustServerCertificate=True",
                sql => sql.MigrationsAssembly(typeof(ENoteContext).Assembly.FullName))
            .Options;

        using var context = new ENoteContext(
            options,
            new FixedClock(new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc)),
            new StubCurrentActor());

        Assert.False(context.Database.HasPendingModelChanges());
    }
}
