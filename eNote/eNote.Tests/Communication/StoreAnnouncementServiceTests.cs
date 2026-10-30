using eNote.Application.Common.Files;
using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Communication.Announcements.Services;
using eNote.Domain.Entities.Communication;
using eNote.Infrastructure.Messaging;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace eNote.Tests.Communication;

public sealed class StoreAnnouncementServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateForStoreAsync_AddsStoreAnnouncement()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, storeId: 3);

        var dto = await service.CreateForStoreAsync(new AnnouncementRequest("Sale", "20% off"));

        Assert.Equal("Sale", dto.Title);
        var row = await harness.Context.Set<Announcement>().AsNoTracking().IgnoreQueryFilters().SingleAsync(a => a.Title == "Sale");
        Assert.Equal(3, row.MusicStoreId);
    }

    [Fact]
    public async Task GetForStoreAsync_FiltersByTitle()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var store = new MusicStore("Music Shop", "09-17");
        harness.Context.Set<MusicStore>().Add(store);
        await harness.Context.SaveChangesAsync();
        harness.Context.Set<Announcement>().AddRange(
            new Announcement("Summer sale", "20% off", null, store.Id, Now),
            new Announcement("New hours", "Open late", null, store.Id, Now));
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, storeId: store.Id);

        var result = await service.GetForStoreAsync(new AnnouncementSearchObject { Title = "Summer", Page = 1, PageSize = 10 });

        Assert.Equal("Summer sale", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task UploadImageForStoreAsync_SetsPath()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var store = new MusicStore("Music Shop", "09-17");
        harness.Context.Set<MusicStore>().Add(store);
        await harness.Context.SaveChangesAsync();
        var announcement = new Announcement("Sale", "20% off", null, store.Id, Now);
        harness.Context.Set<Announcement>().Add(announcement);
        await harness.Context.SaveChangesAsync();
        var fileStorage = new RecordingFileStorageService();
        var service = CreateService(harness.Context, storeId: store.Id, fileStorage: fileStorage);

        var dto = await service.UploadImageForStoreAsync(announcement.Id, new MemoryStream([1, 2, 3]), "image/png");

        Assert.Equal(fileStorage.SavedPaths[0], dto.ImagePath);
        Assert.Equal("Music Shop", dto.StoreName);
    }

    [Fact]
    public async Task UploadImageForStoreAsync_Throws_WhenStoreQuotaExceeded()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var store = new MusicStore("Music Shop", "09-17");
        harness.Context.Set<MusicStore>().Add(store);
        await harness.Context.SaveChangesAsync();
        var existing = new Announcement("Sale", "20% off", null, store.Id, Now);
        existing.UpdateImagePath("/api/v1/uploads/announcements/old.webp");
        var target = new Announcement("New", "News", null, store.Id, Now);
        harness.Context.Set<Announcement>().AddRange(existing, target);
        await harness.Context.SaveChangesAsync();
        var fileStorage = new RecordingFileStorageService();
        fileStorage.FileSizes["/api/v1/uploads/announcements/old.webp"] = FileUploadLimits.MaxStoreUploadBytes;
        var service = CreateService(harness.Context, storeId: store.Id, fileStorage: fileStorage);

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.UploadImageForStoreAsync(target.Id, new MemoryStream([1]), "image/png"));

        Assert.Empty(fileStorage.SavedPaths);
    }

    [Fact]
    public async Task DeleteForStoreAsync_DeletesImageFile()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var store = new MusicStore("Music Shop", "09-17");
        harness.Context.Set<MusicStore>().Add(store);
        await harness.Context.SaveChangesAsync();
        var announcement = new Announcement("Sale", "20% off", null, store.Id, Now);
        harness.Context.Set<Announcement>().Add(announcement);
        await harness.Context.SaveChangesAsync();
        var fileStorage = new RecordingFileStorageService();
        var service = CreateService(harness.Context, storeId: store.Id, fileStorage: fileStorage);

        await service.UploadImageForStoreAsync(announcement.Id, new MemoryStream([1, 2, 3]), "image/png");
        await service.DeleteForStoreAsync(announcement.Id);

        Assert.Contains(fileStorage.SavedPaths[0], fileStorage.DeletedPaths);
    }

    [Fact]
    public async Task CreateForStoreAsync_NotifiesStudentsWithAnActiveStoreRelationship()
    {
        var fixture = await SeedStoreFixtureAsync();

        var dto = await CreateService(fixture.Context, storeId: fixture.Store.Id).CreateForStoreAsync(new AnnouncementRequest("Sale", "20% off"));

        var messages = await ReadPublishedAsync(fixture.Context);

        Assert.Equal([21, 22, 25], messages.Select(m => m.StudentUserId).OrderBy(id => id));
        Assert.All(messages, message =>
        {
            Assert.Equal("Nova obavijest prodavnice", message.Title);
            Assert.Equal("Sale", message.Body);
            Assert.Equal(dto.Id, message.AnnouncementId);
        });
    }

    [Theory]
    [InlineData(InstrumentRentalStatus.Pending)]
    [InlineData(InstrumentRentalStatus.Approved)]
    [InlineData(InstrumentRentalStatus.Rejected)]
    [InlineData(InstrumentRentalStatus.Canceled)]
    [InlineData(InstrumentRentalStatus.Active)]
    [InlineData(InstrumentRentalStatus.Completed)]
    [InlineData(InstrumentRentalStatus.ReturnedEarly)]
    public async Task CreateForStoreAsync_UsesTheSameAudienceRuleAsTheStudentFeed(InstrumentRentalStatus status)
    {
        var databaseName = Guid.NewGuid().ToString();
        var context = TestDbContextFactory.CreateContext(Now, databaseName);
        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var instrument = await AddInstrumentAsync(context, store.Id, "Strat");
        var student = new Student(21, Now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();

        var rental = new InstrumentRental(instrument.Id, student.Id, store.Id, Now, null);
        RentalTestData.AdvanceTo(rental, status, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        context.ExplicitStoreId = store.Id;
        var dto = await CreateService(context, storeId: store.Id).CreateForStoreAsync(new AnnouncementRequest("Sale", "20% off"));

        var published = await ReadPublishedAsync(context);

        var studentContext = TestDbContextFactory.CreateContext(Now, databaseName);
        studentContext.ExplicitStoreId = null;

        var feed = await new StudentAnnouncementFeedService(studentContext, new StubCurrentActor(student: student), TestMapper.Create())
            .GetFeedForStudentAsync(new AnnouncementSearchObject { Page = 1, PageSize = 10 });

        Assert.Equal(published.Count > 0, feed.Items.Any(a => a.Id == dto.Id));
    }

    private static async Task<StoreFixture> SeedStoreFixtureAsync()
    {
        var context = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Music Shop", "09-17");
        var otherStore = new MusicStore("Other Shop", "09-17");
        context.Set<MusicStore>().AddRange(store, otherStore);
        await context.SaveChangesAsync();

        var instrument = await AddInstrumentAsync(context, store.Id, "Strat");
        var otherInstrument = await AddInstrumentAsync(context, otherStore.Id, "Les Paul");

        var students = new List<Student>();
        for (var index = 0; index < 5; index++)
        {
            var student = new Student(21 + index, Now);
            context.Set<Student>().Add(student);
            students.Add(student);
        }

        await context.SaveChangesAsync();

        AddRental(context, instrument, students[0], store.Id, InstrumentRentalStatus.Active);
        AddRental(context, instrument, students[1], store.Id, InstrumentRentalStatus.Completed);
        AddRental(context, instrument, students[2], store.Id, InstrumentRentalStatus.Pending);
        AddRental(context, otherInstrument, students[3], otherStore.Id, InstrumentRentalStatus.Active);
        AddRental(context, instrument, students[4], store.Id, InstrumentRentalStatus.Approved);
        AddRental(context, instrument, students[4], store.Id, InstrumentRentalStatus.ReturnedEarly);

        await context.SaveChangesAsync();
        context.ExplicitStoreId = store.Id;

        return new StoreFixture(context, store);
    }

    private static async Task<Instrument> AddInstrumentAsync(ENoteContext context, int storeId, string model)
    {
        var type = new InstrumentType { Type = $"Type {model}", MonthlyFee = 50m };
        context.Set<InstrumentType>().Add(type);
        await context.SaveChangesAsync();

        var instrument = new Instrument(model, "Fender", null, null, type.Id, storeId);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();

        return instrument;
    }

    private static void AddRental(ENoteContext context, Instrument instrument, Student student, int storeId, InstrumentRentalStatus status)
    {
        var rental = new InstrumentRental(instrument.Id, student.Id, storeId, Now, null);
        RentalTestData.AdvanceTo(rental, status, Now);
        context.Set<InstrumentRental>().Add(rental);
    }

    private static async Task<List<AnnouncementPublished>> ReadPublishedAsync(ENoteContext context)
    {
        var rows = await context.Set<NotificationOutbox>().AsNoTracking().ToListAsync();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        return rows.Select(row => JsonSerializer.Deserialize<AnnouncementPublished>(row.PayloadJson, options)!).ToList();
    }

    private static StoreAnnouncementService CreateService(
        ENoteContext context,
        int storeId = 1,
        StubCurrentActor? actor = null,
        RecordingFileStorageService? fileStorage = null) =>
        new(context,
            new FixedClock(Now),
            actor ?? new StubCurrentActor(storeId: storeId),
            TestMapper.Create(),
            fileStorage ?? new RecordingFileStorageService(),
            new AnnouncementNotificationDispatcher(context, new FixedClock(Now)));

    private sealed record StoreFixture(ENoteContext Context, MusicStore Store);
}
