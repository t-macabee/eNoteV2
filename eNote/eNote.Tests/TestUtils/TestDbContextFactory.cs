using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace eNote.Tests.TestUtils;

public static class TestDbContextFactory
{
    public static DbContextOptions<ENoteContext> CreateOptions(string? databaseName = null)
    {
        databaseName ??= Guid.NewGuid().ToString();

        return new DbContextOptionsBuilder<ENoteContext>()
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .ReplaceService<IModelCustomizer, InMemoryRowVersionModelCustomizer>()
            .Options;
    }

    public static ENoteContext CreateContext(DateTime now, string? databaseName = null)
    {
        return new ENoteContext(CreateOptions(databaseName), new FixedClock(now), new StubCurrentActor(new Student(0, now))) { ExplicitStoreId = 1 };
    }
}
