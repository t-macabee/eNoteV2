using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Contracts.Identity;
using eNote.Tests.TestUtils;
using eNote.Worker.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Tests.Messaging;

public sealed class MembershipWarningEmailConsumerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Consume_SendsTheWarningMail()
    {
        var email = new RecordingEmailService();
        await using var provider = BuildProvider(email, new UserIdentityDto { Id = 5, Username = "student", Email = "student@example.com", IsActive = true });

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

        var notification = Assert.Single(email.Notifications);
        Assert.Equal("student@example.com", notification.Email);
        Assert.Equal("Upozorenje o članarini", notification.Subject);
        Assert.Equal("Vaša članarina ističe.", notification.Body);
    }

    [Fact]
    public async Task Consume_SendsNoMail_WhenRecipientHasNoEmail()
    {
        var email = new RecordingEmailService();
        await using var provider = BuildProvider(email, new UserIdentityDto { Id = 5, Username = "student", Email = null, IsActive = true });

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var message = new MembershipWarning(5, "Upozorenje o članarini", "Vaša članarina ističe.", Now);

            await harness.Bus.Publish(message);

            Assert.True(await harness.Consumed.Any<MembershipWarning>());
            Assert.False(await harness.Published.Any<Fault<MembershipWarning>>());
        }
        finally
        {
            await harness.Stop();
        }

        Assert.Empty(email.Notifications);
    }

    private static ServiceProvider BuildProvider(RecordingEmailService email, UserIdentityDto user)
    {
        var users = new StubUserIdentityService(new Dictionary<int, UserIdentityDto> { [user.Id] = user });
        return new ServiceCollection()
            .AddSingleton<IUserIdentityService>(users)
            .AddSingleton<IEmailService>(email)
            .AddMassTransitTestHarness(bus => bus.AddConsumer<MembershipWarningEmailConsumer>())
            .BuildServiceProvider(true);
    }
}
