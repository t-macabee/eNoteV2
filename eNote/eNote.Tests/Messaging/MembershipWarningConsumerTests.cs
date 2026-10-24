using eNote.Application.Common.Persistence;
using eNote.Contracts.Identity;
using eNote.Domain.Entities.Communication;
using eNote.Tests.TestUtils;
using eNote.Worker.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Tests.Messaging;

public sealed class MembershipWarningConsumerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Consume_StoresTheNotificationForTheStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var provider = new ServiceCollection()
            .AddSingleton<IAppDbContext>(context)
            .AddMassTransitTestHarness(bus => bus.AddConsumer<MembershipWarningConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var message = new MembershipWarning(5, "Upozorenje o članarini", "Vaša članarina ističe.", Now);

            await harness.Bus.Publish(message);

            Assert.True(await harness.Consumed.Any<MembershipWarning>());
        }
        finally
        {
            await harness.Stop();
        }

        var notification = await context.Set<Notification>().SingleAsync();
        Assert.Equal(5, notification.UserId);
        Assert.Equal("Upozorenje o članarini", notification.Title);
        Assert.Equal("Vaša članarina ističe.", notification.Body);
        Assert.Equal(Now, notification.CreatedAt);
    }

    [Fact]
    public async Task Consume_StoresOneNotification_WhenTheSameWarningIsDeliveredTwice()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var provider = new ServiceCollection()
            .AddSingleton<IAppDbContext>(context)
            .AddMassTransitTestHarness(bus => bus.AddConsumer<MembershipWarningConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var message = new MembershipWarning(5, "Upozorenje o članarini", "Vaša članarina ističe.", Now);

            await harness.Bus.Publish(message);
            await harness.Bus.Publish(message);

            Assert.True(await harness.Consumed.Any<MembershipWarning>());
        }
        finally
        {
            await harness.Stop();
        }

        Assert.Single(await context.Set<Notification>().ToListAsync());
    }
}
