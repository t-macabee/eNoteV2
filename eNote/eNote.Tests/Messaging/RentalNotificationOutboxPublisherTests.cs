using eNote.Application.Common.Persistence;
using eNote.Contracts.Communication;
using eNote.Contracts.Enrollments;
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

public sealed class RentalNotificationOutboxPublisherTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ProcessBatch_PublishesPendingMessages_AndMarksPublishedAt()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var outbox = new NotificationOutbox { PayloadJson = "{}" };
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
        await using var context = TestDbContextFactory.CreateContext(Now);
        var message = new RentalStatusChanged(1, 5, 9, "Pending", "Stratocaster", "Title", "Body", Now);
        context.Set<NotificationOutbox>().Add(new NotificationOutbox
        {
            PayloadJson = JsonSerializer.Serialize(message, JsonOptions)
        });
        await context.SaveChangesAsync();
        var endpoint = new StubPublishEndpoint();
        var publisher = CreatePublisher(context, endpoint);

        await InvokeProcessBatchAsync(publisher);

        var published = Assert.Single(endpoint.Published);
        var payload = Assert.IsType<RentalStatusChanged>(published);
        Assert.Equal(1, payload.RentalId);
        Assert.Equal(5, payload.StudentUserId);
    }

    [Fact]
    public async Task ProcessBatch_PublishesLectureCancelledPayloads()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var message = new LectureCancelled(1, 50, "Guitar 101", "Predavanje otkazano", "Predavanje je otkazano.", Now);
        context.Set<NotificationOutbox>().Add(new NotificationOutbox
        {
            MessageType = NotificationMessageTypes.LectureCancelled,
            PayloadJson = JsonSerializer.Serialize(message, JsonOptions)
        });
        await context.SaveChangesAsync();
        var endpoint = new StubPublishEndpoint();
        var publisher = CreatePublisher(context, endpoint);

        await InvokeProcessBatchAsync(publisher);

        var published = Assert.Single(endpoint.Published);
        var payload = Assert.IsType<LectureCancelled>(published);
        Assert.Equal(1, payload.LectureId);
        Assert.Equal(50, payload.StudentUserId);
    }

    [Fact]
    public async Task ProcessBatch_PublishesSubmissionGradedPayloads()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var message = new SubmissionGraded(1, 50, "Homework", 85, "Zadaća ocijenjena", "Ocjena: 85.", Now);
        context.Set<NotificationOutbox>().Add(new NotificationOutbox
        {
            MessageType = NotificationMessageTypes.SubmissionGraded,
            PayloadJson = JsonSerializer.Serialize(message, JsonOptions)
        });
        await context.SaveChangesAsync();
        var endpoint = new StubPublishEndpoint();
        var publisher = CreatePublisher(context, endpoint);

        await InvokeProcessBatchAsync(publisher);

        var published = Assert.Single(endpoint.Published);
        var payload = Assert.IsType<SubmissionGraded>(published);
        Assert.Equal(1, payload.SubmissionId);
        Assert.Equal(85, payload.Grade);
    }

    [Fact]
    public async Task ProcessBatch_PublishesAnnouncementPublishedPayloads()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var message = new AnnouncementPublished(1, 50, "Nova obavijest", "Tekst obavijesti.", Now);
        context.Set<NotificationOutbox>().Add(new NotificationOutbox
        {
            MessageType = NotificationMessageTypes.AnnouncementPublished,
            PayloadJson = JsonSerializer.Serialize(message, JsonOptions)
        });
        await context.SaveChangesAsync();
        var endpoint = new StubPublishEndpoint();
        var publisher = CreatePublisher(context, endpoint);

        await InvokeProcessBatchAsync(publisher);

        var published = Assert.Single(endpoint.Published);
        var payload = Assert.IsType<AnnouncementPublished>(published);
        Assert.Equal(1, payload.AnnouncementId);
        Assert.Equal(50, payload.StudentUserId);
    }

    [Fact]
    public async Task ProcessBatch_PublishesEnrollmentStatusChangedPayloads()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var message = new EnrollmentStatusChanged(1, 50, "Upis odobren", "Vaš zahtjev za upis na kurs 'Guitar 101' je odobren.", Now);
        context.Set<NotificationOutbox>().Add(new NotificationOutbox
        {
            MessageType = NotificationMessageTypes.EnrollmentStatusChanged,
            PayloadJson = JsonSerializer.Serialize(message, JsonOptions)
        });
        await context.SaveChangesAsync();
        var endpoint = new StubPublishEndpoint();
        var publisher = CreatePublisher(context, endpoint);

        await InvokeProcessBatchAsync(publisher);

        var published = Assert.Single(endpoint.Published);
        var payload = Assert.IsType<EnrollmentStatusChanged>(published);
        Assert.Equal(1, payload.EnrollmentId);
        Assert.Equal(50, payload.StudentUserId);
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
        context.Set<NotificationOutbox>().Add(new NotificationOutbox { PayloadJson = "not-json" });
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
        context.Set<NotificationOutbox>().Add(new NotificationOutbox { PayloadJson = "{}", Attempts = 5 });
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
        var publisher = new RentalNotificationOutboxPublisher(provider, NullLogger<RentalNotificationOutboxPublisher>.Instance, TimeSpan.FromMilliseconds(10));

        await publisher.StartAsync(CancellationToken.None);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (throwingContext.SetCalls < 2 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }
        await publisher.StopAsync(CancellationToken.None);

        var executeTask = (Task)typeof(BackgroundService)
            .GetProperty("ExecuteTask", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(publisher)!;
        Assert.True(executeTask.IsCompletedSuccessfully, "ExecuteAsync faulted on a poll failure instead of retrying next tick");
        Assert.True(throwingContext.SetCalls > 1, "the poll was not retried after the first failure");
    }

    private static RentalNotificationOutboxPublisher CreatePublisher(ENoteContext context, StubPublishEndpoint? endpoint = null)
    {
        endpoint ??= new StubPublishEndpoint();
        var provider = new StubServiceProvider(context, endpoint, new FixedClock(Now));

        return new RentalNotificationOutboxPublisher(provider, NullLogger<RentalNotificationOutboxPublisher>.Instance);
    }

    private static Task InvokeProcessBatchAsync(RentalNotificationOutboxPublisher publisher) =>
        publisher.ProcessBatchAsync(CancellationToken.None);

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
        public int SetCalls { get; private set; }

        public DbSet<TEntity> Set<TEntity>() where TEntity : class
        {
            SetCalls++;
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
