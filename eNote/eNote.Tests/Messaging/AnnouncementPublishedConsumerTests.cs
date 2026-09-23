using eNote.Application.Common.Persistence;
using eNote.Contracts.Communication;
using eNote.Domain.Entities.Communication;
using eNote.Tests.TestUtils;
using eNote.Worker.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Tests.Messaging;

public sealed class AnnouncementPublishedConsumerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Consume_StoresNotificationForStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var provider = new ServiceCollection()
            .AddSingleton<IAppDbContext>(context)
            .AddMassTransitTestHarness(bus => bus.AddConsumer<AnnouncementPublishedConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var message = new AnnouncementPublished(9, 5, "Nova obavijest na kursu", "Dobrodošli na kurs", Now);

            await harness.Bus.Publish(message);

            Assert.True(await harness.Consumed.Any<AnnouncementPublished>());
        }
        finally
        {
            await harness.Stop();
        }

        var notification = await context.Set<Notification>().SingleAsync();
        Assert.Equal(5, notification.UserId);
        Assert.Equal(9, notification.AnnouncementId);
        Assert.Equal("Nova obavijest na kursu", notification.Title);
        Assert.Equal("Dobrodošli na kurs", notification.Body);
        Assert.Equal(Now, notification.CreatedAt);
    }
}
