using eNote.Application.Common.Interfaces;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Contracts.Rentals;
using eNote.Tests.TestUtils;
using eNote.Worker.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Tests.Messaging;

public sealed class RentalStatusChangedEmailConsumerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Consume_SendsOneMail_WhenStatusIsApproved()
    {
        var email = new RecordingEmailService();
        await using var provider = BuildProvider(email, new UserIdentityDto { Id = 5, Username = "student", Email = "student@example.com", IsActive = true });

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var message = new RentalStatusChanged(9, 5, 11, "Approved", "Stratocaster", "Zahtjev odobren", "Vaš zahtjev je odobren.", Now);

            await harness.Bus.Publish(message);

            Assert.True(await harness.Consumed.Any<RentalStatusChanged>());
        }
        finally
        {
            await harness.Stop();
        }

        var notification = Assert.Single(email.Notifications);
        Assert.Equal("student@example.com", notification.Email);
        Assert.Equal("Zahtjev odobren", notification.Subject);
        Assert.Equal("Vaš zahtjev je odobren.", notification.Body);
    }

    [Fact]
    public async Task Consume_SendsNoMail_WhenStatusIsActive()
    {
        var email = new RecordingEmailService();
        await using var provider = BuildProvider(email, new UserIdentityDto { Id = 5, Username = "student", Email = "student@example.com", IsActive = true });

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var message = new RentalStatusChanged(9, 5, null, "Active", "Stratocaster", "Status iznajmljivanja", "Iznajmljivanje je aktivirano.", Now);

            await harness.Bus.Publish(message);

            Assert.True(await harness.Consumed.Any<RentalStatusChanged>());
        }
        finally
        {
            await harness.Stop();
        }

        Assert.Empty(email.Notifications);
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
            var message = new RentalStatusChanged(9, 5, 11, "Canceled", "Stratocaster", "Zahtjev otkazan", "Vaš zahtjev je otkazan.", Now);

            await harness.Bus.Publish(message);

            Assert.True(await harness.Consumed.Any<RentalStatusChanged>());
            Assert.False(await harness.Published.Any<Fault<RentalStatusChanged>>());
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
            .AddMassTransitTestHarness(bus => bus.AddConsumer<RentalStatusChangedEmailConsumer>())
            .BuildServiceProvider(true);
    }
}
