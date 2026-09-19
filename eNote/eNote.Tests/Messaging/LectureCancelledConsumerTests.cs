using eNote.Application.Common.Persistence;
using eNote.Contracts.Lectures;
using eNote.Domain.Entities.Communication;
using eNote.Tests.TestUtils;
using eNote.Worker.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Tests.Messaging;

public sealed class LectureCancelledConsumerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Consume_StoresNotificationForStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var provider = new ServiceCollection()
            .AddSingleton<IAppDbContext>(context)
            .AddMassTransitTestHarness(bus => bus.AddConsumer<LectureCancelledConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var message = new LectureCancelled(3, 5, "Harmonija", "Predavanje otkazano", "Predavanje Harmonija je otkazano.", Now);

            await harness.Bus.Publish(message);

            Assert.True(await harness.Consumed.Any<LectureCancelled>());
        }
        finally
        {
            await harness.Stop();
        }

        var notification = await context.Set<Notification>().SingleAsync();
        Assert.Equal(5, notification.UserId);
        Assert.Equal(3, notification.LectureId);
        Assert.Equal("Predavanje otkazano", notification.Title);
        Assert.Equal("Predavanje Harmonija je otkazano.", notification.Body);
        Assert.Equal(Now, notification.CreatedAt);
    }
}
