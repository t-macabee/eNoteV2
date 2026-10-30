using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Contracts.Rentals;
using eNote.Domain.Entities.Communication;
using eNote.Infrastructure.Identity;
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

        var row = await context.Set<NotificationOutbox>().SingleAsync();
        Assert.NotNull(row.PayloadJson);
        Assert.Contains("Zahtjev za iznajmljivanje poslan", row.PayloadJson);
    }

    [Fact]
    public async Task DispatchCreatedAsync_MentionsOtherPendingRequests_WhenThereAreAny()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new RentalNotificationDispatcher(context, new FixedClock(Now));
        var dto = CreateRentalDto();
        dto.OtherPendingRequests = 3;

        await dispatcher.DispatchCreatedAsync(dto, studentUserId: 5);
        await context.SaveChangesAsync();

        var payload = await ReadSingleCreatedPayloadAsync(context);
        Assert.Contains("Broj zahtjeva na čekanju za isti instrument: 3.", payload.Body);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task DispatchCreatedAsync_OmitsPendingCount_WhenNobodyElseWaits(int? otherPending)
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new RentalNotificationDispatcher(context, new FixedClock(Now));
        var dto = CreateRentalDto();
        dto.OtherPendingRequests = otherPending;

        await dispatcher.DispatchCreatedAsync(dto, studentUserId: 5);
        await context.SaveChangesAsync();

        var payload = await ReadSingleCreatedPayloadAsync(context);
        Assert.DoesNotContain("Broj zahtjeva na čekanju", payload.Body);
    }

    private static async Task<RentalStatusChanged> ReadSingleCreatedPayloadAsync(DbContext context)
    {
        var row = await context.Set<NotificationOutbox>().SingleAsync();
        var payload = JsonSerializer.Deserialize<RentalStatusChanged>(row.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        return payload;
    }

    [Fact]
    public async Task DispatchTransitionAsync_UsesRecipientUserId_AndTriggerTitle()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new RentalNotificationDispatcher(context, new FixedClock(Now));

        await dispatcher.DispatchTransitionAsync(CreateRentalDto(), RentalTrigger.Approve, actorUserId: 9);
        await context.SaveChangesAsync();

        var row = await context.Set<NotificationOutbox>().SingleAsync();
        var payload = JsonSerializer.Deserialize<RentalStatusChanged>(row.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        Assert.Equal(5, payload.RecipientUserId);
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

        var row = await context.Set<NotificationOutbox>().SingleAsync();
        Assert.Contains("Not in stock", row.PayloadJson);
    }

    [Fact]
    public async Task DispatchCreatedAsync_SkipsEmployee_WhenAppUserIsNotActive()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);

        var store = new MusicStore("Store", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        context.Set<AppUser>().AddRange(
            new AppUser { Id = 10, UserName = "former", Email = "former@example.com", IsActive = false },
            new AppUser { Id = 11, UserName = "current", Email = "current@example.com", IsActive = true });
        context.Set<MusicStoreEmployee>().AddRange(
            new MusicStoreEmployee(appUserId: 10, musicStoreId: store.Id, isManager: false),
            new MusicStoreEmployee(appUserId: 11, musicStoreId: store.Id, isManager: false));
        await context.SaveChangesAsync();

        var dispatcher = new RentalNotificationDispatcher(context, new FixedClock(Now));
        var dto = CreateRentalDto();
        dto.MusicStoreId = store.Id;

        await dispatcher.DispatchCreatedAsync(dto, studentUserId: 5);
        await context.SaveChangesAsync();

        var messages = (await context.Set<NotificationOutbox>().ToListAsync())
            .Select(row => JsonSerializer.Deserialize<RentalStatusChanged>(row.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!)
            .ToList();
        Assert.Equal([5, 11], messages.Select(m => m.RecipientUserId).Order().ToList());
        Assert.All(messages, m => Assert.Equal(5, m.StudentUserId));
    }

    [Theory]
    [InlineData("bam", "KM")]
    [InlineData("", "KM")]
    [InlineData("eur", "EUR")]
    public async Task DispatchPaymentRefundedAsync_FormatsCurrency(string currency, string expected)
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var dispatcher = new RentalNotificationDispatcher(context, new FixedClock(Now));

        await dispatcher.DispatchPaymentRefundedAsync(CreateRentalDto(), refundedCents: 5000, currency: currency, actorUserId: 9);
        await context.SaveChangesAsync();

        var row = await context.Set<NotificationOutbox>().SingleAsync();
        var payload = JsonSerializer.Deserialize<RentalRefunded>(row.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        Assert.Contains("50", payload.Body);
        Assert.EndsWith($"{expected}.", payload.Body);
        Assert.Equal(currency, payload.Currency);
    }

    [Fact]
    public async Task DispatchTransitionAsync_StudentCancel_NotifiesActiveStoreEmployees_NotStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);

        var store = new MusicStore("Store", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        context.Set<AppUser>().AddRange(
            new AppUser { Id = 10, UserName = "former", Email = "former@example.com", IsActive = false },
            new AppUser { Id = 11, UserName = "current", Email = "current@example.com", IsActive = true });
        context.Set<MusicStoreEmployee>().AddRange(
            new MusicStoreEmployee(appUserId: 10, musicStoreId: store.Id, isManager: false),
            new MusicStoreEmployee(appUserId: 11, musicStoreId: store.Id, isManager: false));
        await context.SaveChangesAsync();

        var dispatcher = new RentalNotificationDispatcher(context, new FixedClock(Now));
        var dto = CreateRentalDto();
        dto.MusicStoreId = store.Id;
        dto.RentalStatus = InstrumentRentalStatus.Canceled;

        await dispatcher.DispatchTransitionAsync(dto, RentalTrigger.Cancel, actorUserId: 5);
        await context.SaveChangesAsync();

        var recipients = (await context.Set<NotificationOutbox>().ToListAsync())
            .Select(row => JsonSerializer.Deserialize<RentalStatusChanged>(row.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!)
            .ToList();
        Assert.Equal([11], recipients.Select(r => r.RecipientUserId).Order().ToList());
        Assert.All(recipients, r => Assert.Equal(5, r.StudentUserId));
        Assert.All(recipients, r => Assert.Equal("Zahtjev otkazan", r.Title));
        Assert.All(recipients, r => Assert.Contains("Student je otkazao", r.Body));
    }

    [Fact]
    public async Task DispatchTransitionAsync_StoreCancel_NotifiesStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);

        var store = new MusicStore("Store", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        context.Set<AppUser>().AddRange(
            new AppUser { Id = 10, UserName = "former", Email = "former@example.com", IsActive = false },
            new AppUser { Id = 11, UserName = "current", Email = "current@example.com", IsActive = true });
        context.Set<MusicStoreEmployee>().AddRange(
            new MusicStoreEmployee(appUserId: 10, musicStoreId: store.Id, isManager: false),
            new MusicStoreEmployee(appUserId: 11, musicStoreId: store.Id, isManager: false));
        await context.SaveChangesAsync();

        var dispatcher = new RentalNotificationDispatcher(context, new FixedClock(Now));
        var dto = CreateRentalDto();
        dto.MusicStoreId = store.Id;
        dto.RentalStatus = InstrumentRentalStatus.Canceled;

        await dispatcher.DispatchTransitionAsync(dto, RentalTrigger.Cancel, actorUserId: 11);
        await context.SaveChangesAsync();

        var recipients = (await context.Set<NotificationOutbox>().ToListAsync())
            .Select(row => JsonSerializer.Deserialize<RentalStatusChanged>(row.PayloadJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!)
            .ToList();
        var recipient = Assert.Single(recipients);
        Assert.Equal([5], recipients.Select(r => r.RecipientUserId));
        Assert.Contains("Vaš zahtjev", recipient.Body);
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
