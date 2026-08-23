using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace eNote.Tests.TestUtils;

public static class TestDbContextFactory
{
    public static ENoteContext CreateContext(DateTime now, string? databaseName = null)
    {
        databaseName ??= Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ENoteContext(options, new FixedClock(now), new StubCurrentActor(new Student(0, now))) { ExplicitStoreId = 1 };
    }
}
