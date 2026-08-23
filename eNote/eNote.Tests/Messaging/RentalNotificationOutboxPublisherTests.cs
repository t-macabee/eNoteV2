using eNote.Application.Common.Persistence;
using eNote.Contracts.Rentals;
using eNote.Domain.Entities.Communication;
using eNote.Infrastructure.Messaging;
using eNote.Tests.TestUtils;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        var outbox = new RentalNotificationOutbox { PayloadJson = "{}" };
        context.Set<RentalNotificationOutbox>().Add(outbox);
        await context.SaveChangesAsync();
        var publisher = CreatePublisher(context);

        await InvokeProcessBatchAsync(publisher);

        var updated = await context.Set<RentalNotificationOutbox>().SingleAsync();
        Assert.Equal(Now, updated.PublishedAt);
        Assert.Equal(0, updated.Attempts);
    }

    [Fact]
    public async Task ProcessBatch_PublishesValidPayloads()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var message = new RentalStatusChanged(1, 5, 9, "Pending", "Stratocaster", "Title", "Body", Now);
        context.Set<RentalNotificationOutbox>().Add(new RentalNotificationOutbox
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
        context.Set<RentalNotificationOutbox>().Add(new RentalNotificationOutbox
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
        context.Set<RentalNotificationOutbox>().Add(new RentalNotificationOutbox
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
    public async Task ProcessBatch_IncrementsAttempts_ForUnknownMessageType()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<RentalNotificationOutbox>().Add(new RentalNotificationOutbox { MessageType = "SomethingUnrecognized", PayloadJson = "{}" });
        await context.SaveChangesAsync();
        var publisher = CreatePublisher(context);

        await InvokeProcessBatchAsync(publisher);

        var updated = await context.Set<RentalNotificationOutbox>().SingleAsync();
        Assert.Equal(1, updated.Attempts);
        Assert.Null(updated.PublishedAt);
        Assert.Contains("SomethingUnrecognized", updated.LastError);
    }

    [Fact]
    public async Task ProcessBatch_IncrementsAttempts_WhenPublishFails()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<RentalNotificationOutbox>().Add(new RentalNotificationOutbox { PayloadJson = "not-json" });
        await context.SaveChangesAsync();
        var publisher = CreatePublisher(context);

        await InvokeProcessBatchAsync(publisher);

        var updated = await context.Set<RentalNotificationOutbox>().SingleAsync();
        Assert.Equal(1, updated.Attempts);
        Assert.Null(updated.PublishedAt);
        Assert.NotNull(updated.LastError);
    }

    [Fact]
    public async Task ProcessBatch_SkipsMessages_AtMaxAttempts()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<RentalNotificationOutbox>().Add(new RentalNotificationOutbox { PayloadJson = "{}", Attempts = 5 });
        await context.SaveChangesAsync();
        var endpoint = new StubPublishEndpoint();
        var publisher = CreatePublisher(context, endpoint);

        await InvokeProcessBatchAsync(publisher);

        Assert.Empty(endpoint.Published);
        var updated = await context.Set<RentalNotificationOutbox>().SingleAsync();
        Assert.Null(updated.PublishedAt);
    }

    private static RentalNotificationOutboxPublisher CreatePublisher(ENoteContext context, StubPublishEndpoint? endpoint = null)
    {
        endpoint ??= new StubPublishEndpoint();
        var provider = new StubServiceProvider(context, endpoint, new FixedClock(Now));

        return new RentalNotificationOutboxPublisher(provider, NullLogger<RentalNotificationOutboxPublisher>.Instance);
    }

    private static Task InvokeProcessBatchAsync(RentalNotificationOutboxPublisher publisher)
    {
        var method = typeof(RentalNotificationOutboxPublisher)
            .GetMethod("ProcessBatchAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (Task)method.Invoke(publisher, [CancellationToken.None])!;
    }

    private sealed class StubServiceProvider(ENoteContext context, StubPublishEndpoint endpoint, IClock clock) : IServiceProvider
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
