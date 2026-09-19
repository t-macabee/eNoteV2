using eNote.Domain.Entities.Communication;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace eNote.Tests.Worker;

public sealed class WorkerActorTests
{
    [Fact]
    public async Task SaveChangesAsync_DoesNotReadUserId_FromWorkerActor()
    {
        var now = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var context = new ENoteContext(options, new FixedClock(now), new ThrowingUserContext());

        context.Set<Notification>().Add(new Notification(5, "Title", "Body", now, rentalId: 1));
        context.Set<NotificationOutbox>().Add(new NotificationOutbox { PayloadJson = "{}" });

        var exception = await Record.ExceptionAsync(() => context.SaveChangesAsync());

        Assert.Null(exception);
    }

    private sealed class ThrowingUserContext : ICurrentUserContext
    {
        public int UserId => throw new NotSupportedException();
        public bool IsAuthenticated => false;
    }
}
