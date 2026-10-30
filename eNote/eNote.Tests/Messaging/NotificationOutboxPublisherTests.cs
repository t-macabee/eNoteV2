using eNote.Application.Common.Persistence;
using eNote.Contracts.Enrollments;
using eNote.Contracts.Identity;
using eNote.Contracts.Rentals;
using eNote.Domain.Entities.Communication;
using eNote.Infrastructure.Messaging;
using eNote.Tests.TestUtils;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;
using System.Text.Json;

namespace eNote.Tests.Messaging;

public sealed class NotificationOutboxPublisherTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(5);

    public static IEnumerable<object[]> RegisteredMessageTypes =>
        NotificationMessageTypes.All.Select(entry => new object[] { entry.MessageType, entry.Name });

    [Theory]
    [MemberData(nameof(RegisteredMessageTypes))]
    public async Task ProcessBatch_PublishesEveryRegisteredMessageType(Type messageType, string expectedName)
    {
        var message = CreateSampleMessage(messageType);
        await using var context = TestDbContextFactory.CreateContext(Now);

        typeof(NotificationOutboxWriter)
            .GetMethod(nameof(NotificationOutboxWriter.Enqueue), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(messageType)
            .Invoke(null, [context, message]);
        await context.SaveChangesAsync();
        var endpoint = new StubPublishEndpoint();

        await InvokeProcessBatchAsync(CreatePublisher(context, endpoint));

        var outbox = await context.Set<NotificationOutbox>().SingleAsync();
        Assert.Equal(expectedName, outbox.MessageType);
        var published = Assert.Single(endpoint.Published);
        Assert.IsType(messageType, published);
        AssertPublishedIds(message, published);
    }

    [Fact]
    public async Task ProcessBatch_PublishesPendingMessages_AndMarksPublishedAt()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var outbox = new NotificationOutbox { MessageType = NotificationMessageTypes.RentalStatusChanged, PayloadJson = "{}" };
        context.Set<NotificationOutbox>().Add(outbox);
        await context.SaveChangesAsync();
        var publisher = CreatePublisher(context);

        await InvokeProcessBatchAsync(publisher);

        var updated = await context.Set<NotificationOutbox>().SingleAsync();
        Assert.Equal(Now, updated.PublishedAt);
        Assert.Equal(0, updated.Attempts);
    }

    [Fact]
    public async Task ProcessBatch_PublishesValidPayloads()
    {
        var message = new RentalStatusChanged(1, 5, 5, 9, "Pending", "Stratocaster", "Title", "Body", Now);

        var payload = await PublishSinglePayloadAsync(message);

        Assert.Equal(1, payload.RentalId);
        Assert.Equal(5, payload.StudentUserId);
    }

    [Fact]
    public async Task ProcessBatch_PublishesLectureCancelledPayloads()
    {
        var message = new LectureCancelled(1, 50, "Guitar 101", "Predavanje otkazano", "Predavanje je otkazano.", Now);

        var payload = await PublishSinglePayloadAsync(message, NotificationMessageTypes.LectureCancelled);

        Assert.Equal(1, payload.LectureId);
        Assert.Equal(50, payload.StudentUserId);
    }

    [Fact]
    public async Task ProcessBatch_PublishesSubmissionGradedPayloads()
    {
        var message = new SubmissionGraded(1, 50, "Homework", 85, "Zadaća ocijenjena", "Ocjena: 85.", Now);

        var payload = await PublishSinglePayloadAsync(message, NotificationMessageTypes.SubmissionGraded);

        Assert.Equal(1, payload.SubmissionId);
        Assert.Equal(85, payload.Grade);
    }

    [Fact]
    public async Task ProcessBatch_PublishesAnnouncementPublishedPayloads()
    {
        var message = new AnnouncementPublished(1, 50, "Nova obavijest", "Tekst obavijesti.", Now);

        var payload = await PublishSinglePayloadAsync(message, NotificationMessageTypes.AnnouncementPublished);

        Assert.Equal(1, payload.AnnouncementId);
        Assert.Equal(50, payload.StudentUserId);
    }

    [Fact]
    public async Task ProcessBatch_PublishesEnrollmentStatusChangedPayloads()
    {
        var message = new EnrollmentStatusChanged(1, 47, 50, "Upis odobren", "Vaš zahtjev za upis na kurs 'Guitar 101' je odobren.", Now);

        var payload = await PublishSinglePayloadAsync(message, NotificationMessageTypes.EnrollmentStatusChanged);

        Assert.Equal(1, payload.EnrollmentId);
        Assert.Equal(47, payload.CourseId);
        Assert.Equal(50, payload.StudentUserId);
    }

    [Fact]
    public async Task ProcessBatch_PublishesTuitionPaymentPayloads()
    {
        var message = new TuitionPaymentNotification(1, 47, 50, "Školarina plaćena", "Školarina za kurs 'Guitar 101' je plaćena.", Now);

        var payload = await PublishSinglePayloadAsync(message, NotificationMessageTypes.TuitionPayment);

        Assert.Equal(1, payload.EnrollmentId);
        Assert.Equal(47, payload.CourseId);
        Assert.Equal(50, payload.StudentUserId);
    }

    [Fact]
    public async Task ProcessBatch_PublishesMembershipWarningPayloads()
    {
        var message = new MembershipWarning(50, "Upozorenje o članarini", "Vaša članarina ističe.", Now);

        var payload = await PublishSinglePayloadAsync(message, NotificationMessageTypes.MembershipWarning);

        Assert.Equal(50, payload.StudentUserId);
        Assert.Equal("Upozorenje o članarini", payload.Title);
    }

    [Fact]
    public async Task ProcessBatch_IncrementsAttempts_ForUnknownMessageType()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<NotificationOutbox>().Add(new NotificationOutbox { MessageType = "SomethingUnrecognized", PayloadJson = "{}" });
        await context.SaveChangesAsync();
        var publisher = CreatePublisher(context);

        await InvokeProcessBatchAsync(publisher);

        var updated = await context.Set<NotificationOutbox>().SingleAsync();
        Assert.Equal(1, updated.Attempts);
        Assert.Null(updated.PublishedAt);
        Assert.Contains("SomethingUnrecognized", updated.LastError);
    }

    [Fact]
    public async Task ProcessBatch_IncrementsAttempts_WhenPublishFails()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<NotificationOutbox>().Add(new NotificationOutbox { MessageType = NotificationMessageTypes.RentalStatusChanged, PayloadJson = "not-json" });
        await context.SaveChangesAsync();
        var publisher = CreatePublisher(context);

        await InvokeProcessBatchAsync(publisher);

        var updated = await context.Set<NotificationOutbox>().SingleAsync();
        Assert.Equal(1, updated.Attempts);
        Assert.Null(updated.PublishedAt);
        Assert.NotNull(updated.LastError);
    }

    [Fact]
    public async Task ProcessBatch_SkipsMessages_AtMaxAttempts()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<NotificationOutbox>().Add(new NotificationOutbox { MessageType = NotificationMessageTypes.RentalStatusChanged, PayloadJson = "{}", Attempts = 5 });
        await context.SaveChangesAsync();
        var endpoint = new StubPublishEndpoint();
        var publisher = CreatePublisher(context, endpoint);

        await InvokeProcessBatchAsync(publisher);

        Assert.Empty(endpoint.Published);
        var updated = await context.Set<NotificationOutbox>().SingleAsync();
        Assert.Null(updated.PublishedAt);
    }

    [Fact]
    public async Task ExecuteAsync_ContinuesAfterPollFailure()
    {
        var throwingContext = new ThrowingSetDbContext();
        var provider = new StubServiceProvider(throwingContext, new StubPublishEndpoint(), new FixedClock(Now));
        var publisher = new NotificationOutboxPublisher(provider, NullLogger<NotificationOutboxPublisher>.Instance, TimeSpan.FromMilliseconds(10));

        await publisher.StartAsync(CancellationToken.None);
        var retried = await Task.WhenAny(throwingContext.SecondPoll, Task.Delay(PollTimeout)) == throwingContext.SecondPoll;
        await publisher.StopAsync(CancellationToken.None);

        var executeTask = (Task)typeof(BackgroundService)
            .GetProperty("ExecuteTask", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(publisher)!;
        Assert.False(executeTask.IsFaulted, "ExecuteAsync faulted on a poll failure instead of retrying next tick");
        Assert.True(executeTask.IsCanceled, "the stopped publisher task is expected to be canceled");
        Assert.True(retried, "the poll was not retried after the first failure");
    }

    private static NotificationOutboxPublisher CreatePublisher(ENoteContext context, StubPublishEndpoint? endpoint = null)
    {
        endpoint ??= new StubPublishEndpoint();
        var provider = new StubServiceProvider(context, endpoint, new FixedClock(Now));

        return new NotificationOutboxPublisher(provider, NullLogger<NotificationOutboxPublisher>.Instance);
    }

    private static async Task<TMessage> PublishSinglePayloadAsync<TMessage>(TMessage message, string? messageType = null)
        where TMessage : class
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var outbox = new NotificationOutbox
        {
            MessageType = messageType ?? NotificationMessageTypes.NameFor(typeof(TMessage)),
            PayloadJson = JsonSerializer.Serialize(message, JsonOptions)
        };

        context.Set<NotificationOutbox>().Add(outbox);
        await context.SaveChangesAsync();
        var endpoint = new StubPublishEndpoint();

        await InvokeProcessBatchAsync(CreatePublisher(context, endpoint));

        return Assert.IsType<TMessage>(Assert.Single(endpoint.Published));
    }

    private static Task InvokeProcessBatchAsync(NotificationOutboxPublisher publisher) =>
        publisher.ProcessBatchAsync(CancellationToken.None);

    private static object CreateSampleMessage(Type messageType) => messageType switch
    {
        Type t when t == typeof(RentalStatusChanged) => new RentalStatusChanged(11, 22, 22, 33, "Pending", "Stratocaster", "Title", "Body", Now),
        Type t when t == typeof(LectureCancelled) => new LectureCancelled(11, 22, "Guitar 101", "Title", "Body", Now),
        Type t when t == typeof(SubmissionGraded) => new SubmissionGraded(11, 22, "Homework", 85, "Title", "Body", Now),
        Type t when t == typeof(RentalRefunded) => new RentalRefunded(11, 22, 33, 100, "EUR", "Stratocaster", "Title", "Body", Now),
        Type t when t == typeof(AnnouncementPublished) => new AnnouncementPublished(11, 22, "Title", "Body", Now),
        Type t when t == typeof(EnrollmentStatusChanged) => new EnrollmentStatusChanged(11, 47, 22, "Title", "Body", Now),
        Type t when t == typeof(TuitionPaymentNotification) => new TuitionPaymentNotification(11, 47, 22, "Title", "Body", Now),
        Type t when t == typeof(MembershipWarning) => new MembershipWarning(22, "Title", "Body", Now),
        Type t when t == typeof(PasswordResetRequested) => new PasswordResetRequested(22, Now),
        _ => throw new InvalidOperationException($"No sample message for '{messageType.FullName}'.")
    };

    private static void AssertPublishedIds(object message, object published)
    {
        switch (message)
        {
            case RentalStatusChanged sent:
                var rental = Assert.IsType<RentalStatusChanged>(published);
                Assert.Equal(sent.RentalId, rental.RentalId);
                Assert.Equal(sent.StudentUserId, rental.StudentUserId);
                break;
            case LectureCancelled sent:
                var lecture = Assert.IsType<LectureCancelled>(published);
                Assert.Equal(sent.LectureId, lecture.LectureId);
                Assert.Equal(sent.StudentUserId, lecture.StudentUserId);
                break;
            case SubmissionGraded sent:
                var submission = Assert.IsType<SubmissionGraded>(published);
                Assert.Equal(sent.SubmissionId, submission.SubmissionId);
                Assert.Equal(sent.Grade, submission.Grade);
                break;
            case RentalRefunded sent:
                var refund = Assert.IsType<RentalRefunded>(published);
                Assert.Equal(sent.RentalId, refund.RentalId);
                Assert.Equal(sent.RefundedCents, refund.RefundedCents);
                break;
            case AnnouncementPublished sent:
                var announcement = Assert.IsType<AnnouncementPublished>(published);
                Assert.Equal(sent.AnnouncementId, announcement.AnnouncementId);
                Assert.Equal(sent.StudentUserId, announcement.StudentUserId);
                break;
            case EnrollmentStatusChanged sent:
                var enrollment = Assert.IsType<EnrollmentStatusChanged>(published);
                Assert.Equal(sent.EnrollmentId, enrollment.EnrollmentId);
                Assert.Equal(sent.StudentUserId, enrollment.StudentUserId);
                break;
            case TuitionPaymentNotification sent:
                var tuition = Assert.IsType<TuitionPaymentNotification>(published);
                Assert.Equal(sent.EnrollmentId, tuition.EnrollmentId);
                Assert.Equal(sent.StudentUserId, tuition.StudentUserId);
                break;
            case MembershipWarning sent:
                var warning = Assert.IsType<MembershipWarning>(published);
                Assert.Equal(sent.StudentUserId, warning.StudentUserId);
                break;
            case PasswordResetRequested sent:
                var reset = Assert.IsType<PasswordResetRequested>(published);
                Assert.Equal(sent.UserId, reset.UserId);
                break;
            default:
                throw new InvalidOperationException($"No id assertions for '{message.GetType().FullName}'.");
        }
    }

    private sealed class StubServiceProvider(IAppDbContext context, StubPublishEndpoint endpoint, IClock clock) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceScopeFactory))
            {
                return new StubScopeFactory(this);
            }

            if (serviceType == typeof(IAppDbContext))
            {
                return context;
            }

            if (serviceType == typeof(IPublishEndpoint))
            {
                return endpoint;
            }

            if (serviceType == typeof(IClock))
            {
                return clock;
            }

            return null;
        }
    }

    private sealed class ThrowingSetDbContext : IAppDbContext
    {
        private readonly TaskCompletionSource _secondPoll = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _setCalls;

        public Task SecondPoll => _secondPoll.Task;

        public DbSet<TEntity> Set<TEntity>() where TEntity : class
        {
            if (++_setCalls == 2)
            {
                _secondPoll.TrySetResult();
            }

            throw new InvalidOperationException("outbox poll failed");
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubScopeFactory(IServiceProvider provider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new StubScope(provider);
    }

    private sealed class StubScope(IServiceProvider provider) : IServiceScope
    {
        public IServiceProvider ServiceProvider => provider;
        public void Dispose() { }
    }
}
