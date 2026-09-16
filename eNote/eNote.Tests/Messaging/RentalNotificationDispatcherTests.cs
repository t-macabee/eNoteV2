using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Contracts.Rentals;
using eNote.Domain.Entities.Communication;
using eNote.Infrastructure.Messaging;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace eNote.Tests.Messaging;

public sealed class RentalNotificationDispatcherTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task DispatchCreatedAsync_AddsOutboxRow()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new RentalNotificationDispatcher(context, new FixedClock(Now));

        await dispatcher.DispatchCreatedAsync(CreateRentalDto(), studentUserId: 5);
        await context.SaveChangesAsync();

        var row = await context.Set<RentalNotificationOutbox>().SingleAsync();
        Assert.NotNull(row.PayloadJson);
        Assert.Contains("Zahtjev za iznajmljivanje poslan", row.PayloadJson);
    }

    [Fact]
    public async Task DispatchTransitionAsync_UsesStudentUserId_AndTriggerTitle()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new RentalNotificationDispatcher(context, new FixedClock(Now));

        await dispatcher.DispatchTransitionAsync(CreateRentalDto(), RentalTrigger.Approve, actorUserId: 9);
        await context.SaveChangesAsync();

        var row = await context.Set<RentalNotificationOutbox>().SingleAsync();
        var payload = JsonSerializer.Deserialize<RentalStatusChanged>(row.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        Assert.Equal(5, payload.StudentUserId);
        Assert.Equal(9, payload.ActorUserId);
        Assert.Equal("Zahtjev odobren", payload.Title);
    }

    [Fact]
    public async Task DispatchTransitionAsync_Reject_IncludesNote_WhenPresent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new RentalNotificationDispatcher(context, new FixedClock(Now));
        var dto = CreateRentalDto();
        dto.Note = "Not in stock";

        await dispatcher.DispatchTransitionAsync(dto, RentalTrigger.Reject, actorUserId: 9);
        await context.SaveChangesAsync();

        var row = await context.Set<RentalNotificationOutbox>().SingleAsync();
        Assert.Contains("Not in stock", row.PayloadJson);
    }

    [Theory]
    [InlineData("bam", "KM")]
    [InlineData("eur", "EUR")]
    public async Task DispatchPaymentRefundedAsync_FormatsCurrency(string currency, string expected)
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new RentalNotificationDispatcher(context, new FixedClock(Now));

        await dispatcher.DispatchPaymentRefundedAsync(CreateRentalDto(), refundedCents: 5000, currency: currency, actorUserId: 9);
        await context.SaveChangesAsync();

        var row = await context.Set<RentalNotificationOutbox>().SingleAsync();
        var payload = JsonSerializer.Deserialize<RentalRefunded>(row.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        Assert.Contains("50", payload.Body);
        Assert.EndsWith($"{expected}.", payload.Body);
        Assert.Equal(currency, payload.Currency);
    }

    private static InstrumentRentalDto CreateRentalDto() => new()
    {
        Id = 1,
        StudentUserId = 5,
        RentalStatus = InstrumentRentalStatus.Pending,
        InstrumentModel = "Stratocaster",
        StoreName = "Music Shop",
        Fee = 50m
    };
}
