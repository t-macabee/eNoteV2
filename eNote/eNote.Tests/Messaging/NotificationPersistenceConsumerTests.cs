using eNote.Application.Common.Persistence;
using eNote.Domain.Entities.Communication;
using eNote.Tests.TestUtils;
using eNote.Worker.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace eNote.Tests.Messaging;

public sealed class NotificationPersistenceConsumerTests
{
    private const string ConstraintName = "IX_NotificationPersistenceDuplicateSkipTest";
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Consume_SkipsDuplicate_WhenUniqueViolation()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var inner = new Exception($"duplicate key value violates unique constraint \"{ConstraintName}\"");
        var logger = new CapturingLogger<DuplicateSkipConsumer>();

        var provider = new ServiceCollection()
            .AddSingleton<IAppDbContext>(
                new ThrowingSaveDbContext(context, new DbUpdateException("Unique constraint violated.", inner)))
            .AddMassTransitTestHarness(bus => bus.AddConsumer<DuplicateSkipConsumer>())
            .AddSingleton<ILogger<DuplicateSkipConsumer>>(logger)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            await harness.Bus.Publish(new DuplicateSkipMessage(7));

            Assert.True(await harness.Consumed.Any<DuplicateSkipMessage>());
        }
        finally
        {
            await harness.Stop();
        }

        var warning = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Contains("Skipping duplicate duplicate-skip notification", warning.Message);
        Assert.DoesNotContain(logger.Entries, entry => entry.Level == LogLevel.Information);
        Assert.Empty(await context.Set<Notification>().ToListAsync());
    }

    public sealed record DuplicateSkipMessage(int UserId);

    public sealed class DuplicateSkipConsumer(IAppDbContext dbContext, ILogger<DuplicateSkipConsumer> logger)
        : NotificationPersistenceConsumer<DuplicateSkipMessage>(dbContext, logger)
    {
        protected override NotificationWrite Map(DuplicateSkipMessage message) => new(
            new Notification(message.UserId, "Naslov", "Telo", Now, null),
            "duplicate-skip",
            "test",
            message.UserId,
            ConstraintName);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
