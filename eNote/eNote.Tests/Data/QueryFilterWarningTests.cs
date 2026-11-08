using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace eNote.Tests.Data;

public sealed class QueryFilterWarningTests
{
    [Fact]
    public void Model_HasNoRequiredNavigationToAFilteredEntity()
    {
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseSqlServer("Server=localhost;Database=query-filter-check;Trusted_Connection=True;TrustServerCertificate=True")
            .ConfigureWarnings(w => w.Throw(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
            .Options;

        using var context = new ENoteContext(
            options,
            new FixedClock(new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc)),
            new StubCurrentActor());

        var exception = Record.Exception(() => _ = context.Model);

        Assert.Null(exception);
    }
}
