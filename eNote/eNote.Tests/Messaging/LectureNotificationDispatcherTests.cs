using eNote.Contracts.Lectures;
using eNote.Domain.Entities.Communication;
using eNote.Infrastructure.Messaging;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace eNote.Tests.Messaging;

public sealed class LectureNotificationDispatcherTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task DispatchCancelledAsync_IncludesReasonInBody()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new LectureNotificationDispatcher(context, new FixedClock(Now));

        await dispatcher.DispatchCancelledAsync(3, "Harmonija", "Instrument je pokvaren", [5]);
        await context.SaveChangesAsync();

        var row = await context.Set<NotificationOutbox>().SingleAsync();
        var payload = JsonSerializer.Deserialize<LectureCancelled>(row.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        Assert.Equal(5, payload.StudentUserId);
        Assert.Equal(3, payload.LectureId);
        Assert.Equal("Predavanje otkazano", payload.Title);
        Assert.Equal("Predavanje 'Harmonija' je otkazano. Razlog: Instrument je pokvaren", payload.Body);
        Assert.Equal(Now, payload.OccurredAtUtc);
    }

    [Fact]
    public async Task DispatchCancelledAsync_EnqueuesOneRowPerStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new LectureNotificationDispatcher(context, new FixedClock(Now));

        await dispatcher.DispatchCancelledAsync(3, "Harmonija", "Instrument je pokvaren", [5, 6]);
        await context.SaveChangesAsync();

        Assert.Equal(2, await context.Set<NotificationOutbox>().CountAsync());
    }
}
