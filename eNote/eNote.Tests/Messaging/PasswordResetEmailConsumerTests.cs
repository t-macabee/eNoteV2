using eNote.Contracts.Identity;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using eNote.Worker.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Tests.Messaging;

public sealed class PasswordResetEmailConsumerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Consume_SendsOneMailWithAValidToken()
    {
        var (provider, email) = await BuildAsync((7, "student@example.com", true));

        await ConsumeAsync(provider, new PasswordResetRequested(7, Now));

        var mail = Assert.Single(email.PasswordResets);
        Assert.Equal("student@example.com", mail.Email);

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ENoteContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await context.Users.SingleAsync(x => x.Id == 7);

        Assert.True(await userManager.VerifyUserTokenAsync(
            user,
            TokenOptions.DefaultProvider,
            UserManager<AppUser>.ResetPasswordTokenPurpose,
            mail.Token));
    }

    [Fact]
    public async Task Consume_SendsNoMail_WhenRecipientIsInactive()
    {
        var (provider, email) = await BuildAsync((7, "student@example.com", false));

        await ConsumeAsync(provider, new PasswordResetRequested(7, Now));

        Assert.Empty(email.PasswordResets);
    }

    [Fact]
    public async Task Consume_SendsNoMail_WhenRecipientDoesNotExist()
    {
        var (provider, email) = await BuildAsync();

        await ConsumeAsync(provider, new PasswordResetRequested(9, Now));

        Assert.Empty(email.PasswordResets);
    }

    [Fact]
    public async Task Consume_SendsNoMail_WhenRecipientHasNoEmail()
    {
        var (provider, email) = await BuildAsync((9, null, true));

        await ConsumeAsync(provider, new PasswordResetRequested(9, Now));

        Assert.Empty(email.PasswordResets);
    }

    private static async Task<(ServiceProvider Provider, RecordingEmailService Email)> BuildAsync(
        params (int Id, string? Email, bool Active)[] users)
    {
        var email = new RecordingEmailService();
        var context = TestDbContextFactory.CreateContext(Now);

        var services = new ServiceCollection();
        IdentityTestHarness.AddIdentityServices(services);
        services.AddSingleton(context);
        services.AddSingleton<IEmailService>(email);
        services.AddMassTransitTestHarness(bus => bus.AddConsumer<PasswordResetEmailConsumer>());

        var provider = services.BuildServiceProvider(true);

        using var seedScope = provider.CreateScope();
        var userManager = seedScope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        foreach (var (id, mail, active) in users)
        {
            var result = await userManager.CreateAsync(
                new AppUser { Id = id, UserName = $"user{id}", Email = mail, IsActive = active },
                "Password1!");

            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return (provider, email);
    }

    private static async Task ConsumeAsync(ServiceProvider provider, PasswordResetRequested message)
    {
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            await harness.Bus.Publish(message);

            Assert.True(await harness.Consumed.Any<PasswordResetRequested>());
        }
        finally
        {
            await harness.Stop();
        }
    }
}
