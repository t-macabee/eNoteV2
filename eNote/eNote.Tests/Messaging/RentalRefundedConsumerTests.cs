using eNote.Application.Common.Persistence;
using eNote.Contracts.Rentals;
using eNote.Domain.Entities.Communication;
using eNote.Tests.TestUtils;
using eNote.Worker.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Tests.Messaging;

public sealed class RentalRefundedConsumerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Consume_StoresNotificationForStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var provider = new ServiceCollection()
            .AddSingleton<IAppDbContext>(context)
            .AddMassTransitTestHarness(bus => bus.AddConsumer<RentalRefundedConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var message = new RentalRefunded(1, 5, 9, 5000, "bam", "Stratocaster", "Uplata vraćena", "Za iznajmljivanje instrumenta Stratocaster vraćeno je 50,00 KM.", Now);

            await harness.Bus.Publish(message);

            Assert.True(await harness.Consumed.Any<RentalRefunded>());
        }
        finally
        {
            await harness.Stop();
        }

        var notification = await context.Set<Notification>().SingleAsync();
        Assert.Equal(5, notification.UserId);
        Assert.Equal(1, notification.RentalId);
        Assert.Equal("Uplata vraćena", notification.Title);
        Assert.Equal("Za iznajmljivanje instrumenta Stratocaster vraćeno je 50,00 KM.", notification.Body);
        Assert.Equal(Now, notification.CreatedAt);
    }
}
