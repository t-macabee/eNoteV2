using eNote.Contracts.Identity;
using eNote.Domain.Entities.Communication;
using eNote.Infrastructure.Messaging;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace eNote.Tests.Messaging;

public sealed class MembershipWarningDispatcherTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task DispatchWarningAsync_EnqueuesWarningWithTheMembershipEndInTheBody()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new MembershipWarningDispatcher(context, new FixedClock(Now));

        await dispatcher.DispatchWarningAsync(5, Now.Date.AddDays(5), null);
        await context.SaveChangesAsync();

        var row = await context.Set<NotificationOutbox>().SingleAsync();
        Assert.Equal("MembershipWarning", row.MessageType);

        var payload = JsonSerializer.Deserialize<MembershipWarning>(row.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        Assert.Equal(5, payload.StudentUserId);
        Assert.Equal("Upozorenje o članarini", payload.Title);
        Assert.Equal("Vaša članarina ističe 06.07.2026. Obnovite je da biste nastavili koristiti usluge.", payload.Body);
        Assert.Equal(Now, payload.OccurredAtUtc);
    }

    [Fact]
    public async Task DispatchWarningAsync_UsesTheGivenMessage()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new MembershipWarningDispatcher(context, new FixedClock(Now));

        await dispatcher.DispatchWarningAsync(5, null, "  Obnovite članarinu do petka.  ");
        await context.SaveChangesAsync();

        var row = await context.Set<NotificationOutbox>().SingleAsync();
        var payload = JsonSerializer.Deserialize<MembershipWarning>(row.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("Obnovite članarinu do petka.", payload!.Body);
    }
}
