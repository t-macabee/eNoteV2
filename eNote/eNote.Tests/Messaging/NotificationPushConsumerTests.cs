using eNote.API.Realtime.Consumers;
using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using eNote.Contracts.Enrollments;
using eNote.Contracts.Identity;
using eNote.Contracts.Rentals;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace eNote.Tests.Messaging;

public sealed class NotificationPushConsumerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RentalStatusChanged_PushesToTheStudentGroup()
    {
        var (group, dto, messages) = await ConsumeAsync<RentalStatusChangedPushConsumer, RentalStatusChanged>(
            new RentalStatusChanged(9, 5, null, "Active", "Stratocaster", "Status iznajmljivanja", "Iznajmljivanje je aktivirano.", Now));

        Assert.Equal("user:5", group);
        Assert.Equal(9, dto.RentalId);
        Assert.Equal("Status iznajmljivanja", dto.Title);
        Assert.Equal("Iznajmljivanje je aktivirano.", dto.Body);
        Assert.Contains("Pushed rental notification to SignalR group for user 5, rental 9.", messages);
    }

    [Fact]
    public async Task RentalRefunded_PushesToTheStudentGroup()
    {
        var (group, dto, messages) = await ConsumeAsync<RentalRefundedPushConsumer, RentalRefunded>(
            new RentalRefunded(1, 5, 9, 5000, "bam", "Stratocaster", "Uplata vraćena", "Za iznajmljivanje instrumenta Stratocaster vraćeno je 50,00 KM.", Now));

        Assert.Equal("user:5", group);
        Assert.Equal(1, dto.RentalId);
        Assert.Equal("Uplata vraćena", dto.Title);
        Assert.Contains("Pushed rental notification to SignalR group for user 5, rental 1.", messages);
    }

    [Fact]
    public async Task LectureCancelled_PushesToTheStudentGroup()
    {
        var (group, dto, messages) = await ConsumeAsync<LectureCancelledPushConsumer, LectureCancelled>(
            new LectureCancelled(3, 5, "Harmonija", "Predavanje otkazano", "Predavanje Harmonija je otkazano.", Now));

        Assert.Equal("user:5", group);
        Assert.Equal(3, dto.LectureId);
        Assert.Equal("Predavanje otkazano", dto.Title);
        Assert.Contains("Pushed lecture-cancelled notification to SignalR group for user 5, lecture 3.", messages);
    }

    [Fact]
    public async Task SubmissionGraded_PushesToTheStudentGroup()
    {
        var (group, dto, messages) = await ConsumeAsync<SubmissionGradedPushConsumer, SubmissionGraded>(
            new SubmissionGraded(4, 5, "Zadatak 1", 5, "Zadatak ocijenjen", "Zadatak 1 je ocijenjen ocjenom 5.", Now));

        Assert.Equal("user:5", group);
        Assert.Equal(4, dto.SubmissionId);
        Assert.Equal("Zadatak ocijenjen", dto.Title);
        Assert.Contains("Pushed submission-graded notification to SignalR group for user 5, submission 4.", messages);
    }

    [Fact]
    public async Task AnnouncementPublished_PushesToTheStudentGroup()
    {
        var (group, dto, messages) = await ConsumeAsync<AnnouncementPublishedPushConsumer, AnnouncementPublished>(
            new AnnouncementPublished(9, 5, "Nova obavijest", "Tekst obavijesti", Now));

        Assert.Equal("user:5", group);
        Assert.Equal(9, dto.AnnouncementId);
        Assert.Equal("Nova obavijest", dto.Title);
        Assert.Contains("Pushed announcement-published notification to SignalR group for user 5, announcement 9.", messages);
    }

    [Fact]
    public async Task EnrollmentStatusChanged_PushesToTheStudentGroup()
    {
        var (group, dto, messages) = await ConsumeAsync<EnrollmentStatusChangedPushConsumer, EnrollmentStatusChanged>(
            new EnrollmentStatusChanged(9, 5, "Upis odobren", "Vaš zahtjev za upis na kurs 'Guitar 101' je odobren.", Now));

        Assert.Equal("user:5", group);
        Assert.Equal("Upis odobren", dto.Title);
        Assert.Contains("Pushed enrollment-status-changed notification to SignalR group for user 5, enrollment 9.", messages);
    }

    [Fact]
    public async Task TuitionPayment_PushesToTheStudentGroup()
    {
        var (group, dto, messages) = await ConsumeAsync<TuitionPaymentPushConsumer, TuitionPaymentNotification>(
            new TuitionPaymentNotification(9, 5, "Školarina plaćena", "Školarina za kurs 'Guitar 101' je plaćena.", Now));

        Assert.Equal("user:5", group);
        Assert.Equal("Školarina plaćena", dto.Title);
        Assert.Contains("Pushed tuition-payment notification to SignalR group for user 5, enrollment 9.", messages);
    }

    [Fact]
    public async Task MembershipWarning_PushesToTheStudentGroup()
    {
        var (group, dto, messages) = await ConsumeAsync<MembershipWarningPushConsumer, MembershipWarning>(
            new MembershipWarning(5, "Upozorenje o članarini", "Vaša članarina ističe.", Now));

        Assert.Equal("user:5", group);
        Assert.Equal("Upozorenje o članarini", dto.Title);
        Assert.Equal("Vaša članarina ističe.", dto.Body);
        Assert.Contains("Pushed membership-warning notification to SignalR group for user 5, user 5.", messages);
    }

    private static async Task<(string Group, NotificationPushDto Dto, List<string> Messages)> ConsumeAsync<TConsumer, TMessage>(TMessage message)
        where TConsumer : class, IConsumer<TMessage>
        where TMessage : class
    {
        var harness = await StartAsync<TConsumer, TMessage>();
        try
        {
            await harness.Harness.Bus.Publish(message);

            Assert.True(await harness.Harness.Consumed.Any<TMessage>());
        }
        finally
        {
            await harness.Harness.Stop();
            await harness.Provider.DisposeAsync();
        }

        var (group, dto) = AssertPush(harness);
        return (group, dto, harness.Messages);
    }

    private static (string Group, NotificationPushDto Dto) AssertPush(PushHarness harness)
    {
        var send = Assert.Single(harness.Hub.Sends);
        Assert.Equal(NotificationHub.ReceiveMethod, send.Method);
        var dto = Assert.IsType<NotificationPushDto>(send.Payload);
        Assert.Equal(Now, dto.CreatedAt);
        return (send.Group, dto);
    }

    private static async Task<PushHarness> StartAsync<TConsumer, TMessage>()
        where TConsumer : class, IConsumer<TMessage>
        where TMessage : class
    {
        var hub = new RecordingHubContext();
        var messages = new List<string>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHubContext<NotificationHub>>(hub);
        services.AddSingleton(messages);
        services.AddSingleton(typeof(ILogger<>), typeof(RecordingLogger<>));
        services.AddMassTransitTestHarness(bus => bus.AddConsumer<TConsumer>());

        var provider = services.BuildServiceProvider(true);
        var testHarness = provider.GetRequiredService<ITestHarness>();
        await testHarness.Start();

        return new PushHarness(provider, testHarness, hub, messages);
    }

    private sealed record PushHarness(ServiceProvider Provider, ITestHarness Harness, RecordingHubContext Hub, List<string> Messages);

    private sealed class RecordingHubContext : IHubContext<NotificationHub>
    {
        public List<(string Group, string Method, object? Payload)> Sends { get; } = [];

        public IHubClients Clients { get; }

        public IGroupManager Groups => throw new NotSupportedException();

        public RecordingHubContext() => Clients = new RecordingHubClients(this);

        private sealed class RecordingHubClients(RecordingHubContext owner) : IHubClients
        {
            public IClientProxy Group(string groupName) => new RecordingClientProxy(owner, groupName);

            public IClientProxy All => throw new NotSupportedException();

            public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();

            public IClientProxy Client(string connectionId) => throw new NotSupportedException();

            public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();

            public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();

            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();

            public IClientProxy User(string userId) => throw new NotSupportedException();

            public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
        }

        private sealed class RecordingClientProxy(RecordingHubContext owner, string group) : IClientProxy
        {
            public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
            {
                owner.Sends.Add((group, method, args.Length > 0 ? args[0] : null));
                return Task.CompletedTask;
            }
        }
    }

    private sealed class RecordingLogger<T>(List<string> messages) : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => messages.Add(formatter(state, exception));
    }
}
