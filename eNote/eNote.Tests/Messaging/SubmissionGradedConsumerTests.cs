using eNote.Application.Common.Persistence;
using eNote.Contracts.Assignments;
using eNote.Domain.Entities.Communication;
using eNote.Tests.TestUtils;
using eNote.Worker.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Tests.Messaging;

public sealed class SubmissionGradedConsumerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Consume_StoresNotificationForStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var provider = new ServiceCollection()
            .AddSingleton<IAppDbContext>(context)
            .AddMassTransitTestHarness(bus => bus.AddConsumer<SubmissionGradedConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var message = new SubmissionGraded(4, 5, "Zadatak 1", 5, "Zadatak ocijenjen", "Zadatak 1 je ocijenjen ocjenom 5.", Now);

            await harness.Bus.Publish(message);

            Assert.True(await harness.Consumed.Any<SubmissionGraded>());
        }
        finally
        {
            await harness.Stop();
        }

        var notification = await context.Set<Notification>().SingleAsync();
        Assert.Equal(5, notification.UserId);
        Assert.Equal(4, notification.SubmissionId);
        Assert.Equal("Zadatak ocijenjen", notification.Title);
        Assert.Equal("Zadatak 1 je ocijenjen ocjenom 5.", notification.Body);
        Assert.Equal(Now, notification.CreatedAt);
    }
}
