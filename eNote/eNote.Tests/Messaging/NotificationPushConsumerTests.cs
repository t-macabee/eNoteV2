using eNote.API.Realtime.Consumers;
using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using eNote.Contracts.Assignments;
using eNote.Contracts.Communication;
using eNote.Contracts.Enrollments;
using eNote.Contracts.Lectures;
using eNote.Contracts.Rentals;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace eNote.Tests.Messaging;

/// Covers the shared SignalR push owned by NotificationPushConsumer<TMessage>:
/// the target group, the payload each consumer maps, and the log line with the
/// notification kind and reference.
public sealed class NotificationPushConsumerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RentalStatusChanged_PushesToTheStudentGroup()
    {
        var harness = await StartAsync<RentalStatusChangedPushConsumer, RentalStatusChanged>();
        try
        {
            await harness.Harness.Bus.Publish(new RentalStatusChanged(9, 5, null, "Active", "Stratocaster", "Status iznajmljivanja", "Iznajmljivanje je aktivirano.", Now));
            Assert.True(await harness.Harness.Consumed.Any<RentalStatusChanged>());
        }
        finally
        {
            await harness.Harness.Stop();
        }

        var (group, method, dto) = AssertPush(harness);
        Assert.Equal("user:5", group);
        Assert.Equal(9, dto.RentalId);
        Assert.Equal("Status iznajmljivanja", dto.Title);
        Assert.Equal("Iznajmljivanje je aktivirano.", dto.Body);
        Assert.Contains("Pushed rental notification to SignalR group for user 5, rental 9.", harness.Messages);
    }

    [Fact]
    public async Task RentalRefunded_PushesToTheStudentGroup()
    {
        var harness = await StartAsync<RentalRefundedPushConsumer, RentalRefunded>();
        try
        {
            await harness.Harness.Bus.Publish(new RentalRefunded(1, 5, 9, 5000, "bam", "Stratocaster", "Uplata vraćena", "Za iznajmljivanje instrumenta Stratocaster vraćeno je 50,00 KM.", Now));
            Assert.True(await harness.Harness.Consumed.Any<RentalRefunded>());
        }
        finally
        {
            await harness.Harness.Stop();
        }

        var (group, method, dto) = AssertPush(harness);
        Assert.Equal("user:5", group);
        Assert.Equal(1, dto.RentalId);
        Assert.Equal("Uplata vraćena", dto.Title);
        Assert.Contains("Pushed rental notification to SignalR group for user 5, rental 1.", harness.Messages);
    }

    [Fact]
    public async Task LectureCancelled_PushesToTheStudentGroup()
    {
        var harness = await StartAsync<LectureCancelledPushConsumer, LectureCancelled>();
        try
        {
            await harness.Harness.Bus.Publish(new LectureCancelled(3, 5, "Harmonija", "Predavanje otkazano", "Predavanje Harmonija je otkazano.", Now));
            Assert.True(await harness.Harness.Consumed.Any<LectureCancelled>());
        }
        finally
        {
            await harness.Harness.Stop();
        }

        var (group, method, dto) = AssertPush(harness);
        Assert.Equal("user:5", group);
        Assert.Equal(3, dto.LectureId);
        Assert.Equal("Predavanje otkazano", dto.Title);
        Assert.Contains("Pushed lecture-cancelled notification to SignalR group for user 5, lecture 3.", harness.Messages);
    }

    [Fact]
    public async Task SubmissionGraded_PushesToTheStudentGroup()
    {
        var harness = await StartAsync<SubmissionGradedPushConsumer, SubmissionGraded>();
        try
        {
            await harness.Harness.Bus.Publish(new SubmissionGraded(4, 5, "Zadatak 1", 5, "Zadatak ocijenjen", "Zadatak 1 je ocijenjen ocjenom 5.", Now));
            Assert.True(await harness.Harness.Consumed.Any<SubmissionGraded>());
        }
        finally
        {
            await harness.Harness.Stop();
        }

        var (group, method, dto) = AssertPush(harness);
        Assert.Equal("user:5", group);
        Assert.Equal(4, dto.SubmissionId);
        Assert.Equal("Zadatak ocijenjen", dto.Title);
        Assert.Contains("Pushed submission-graded notification to SignalR group for user 5, submission 4.", harness.Messages);
    }

    [Fact]
    public async Task AnnouncementPublished_PushesToTheStudentGroup()
    {
        var harness = await StartAsync<AnnouncementPublishedPushConsumer, AnnouncementPublished>();
        try
        {
            await harness.Harness.Bus.Publish(new AnnouncementPublished(9, 5, "Nova obavijest", "Tekst obavijesti", Now));
            Assert.True(await harness.Harness.Consumed.Any<AnnouncementPublished>());
        }
        finally
        {
            await harness.Harness.Stop();
        }

        var (group, method, dto) = AssertPush(harness);
        Assert.Equal("user:5", group);
        Assert.Equal(9, dto.AnnouncementId);
        Assert.Equal("Nova obavijest", dto.Title);
        Assert.Contains("Pushed announcement-published notification to SignalR group for user 5, announcement 9.", harness.Messages);
    }

    [Fact]
    public async Task EnrollmentStatusChanged_PushesToTheStudentGroup()
    {
        var harness = await StartAsync<EnrollmentStatusChangedPushConsumer, EnrollmentStatusChanged>();
        try
        {
            await harness.Harness.Bus.Publish(new EnrollmentStatusChanged(9, 5, "Upis odobren", "Vaš zahtjev za upis na kurs 'Guitar 101' je odobren.", Now));
            Assert.True(await harness.Harness.Consumed.Any<EnrollmentStatusChanged>());
        }
        finally
        {
            await harness.Harness.Stop();
        }

        var (group, method, dto) = AssertPush(harness);
        Assert.Equal("user:5", group);
        Assert.Equal("Upis odobren", dto.Title);
        Assert.Contains("Pushed enrollment-status-changed notification to SignalR group for user 5, enrollment 9.", harness.Messages);
    }

    private static (string Group, string Method, NotificationPushDto Dto) AssertPush(PushHarness harness)
    {
        var send = Assert.Single(harness.Hub.Sends);
        Assert.Equal(NotificationHub.ReceiveMethod, send.Method);
        var dto = Assert.IsType<NotificationPushDto>(send.Payload);
        Assert.Equal(Now, dto.CreatedAt);
        return (send.Group, send.Method, dto);
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

        return new PushHarness(testHarness, hub, messages);
    }

    private sealed record PushHarness(ITestHarness Harness, RecordingHubContext Hub, List<string> Messages);

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
