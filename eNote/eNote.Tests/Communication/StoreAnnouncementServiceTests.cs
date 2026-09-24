using eNote.Application.Common.Files;
using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Communication.Announcements.Services;
using eNote.Domain.Entities.Communication;
using eNote.Domain.Entities.Rentals;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Communication;

public sealed class StoreAnnouncementServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateForStoreAsync_AddsStoreAnnouncement()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor, storeId: 3);

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
        var service = CreateService(harness.Context, harness.Instructor, storeId: store.Id);

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
        var service = CreateService(harness.Context, harness.Instructor, storeId: store.Id, fileStorage: fileStorage);

        var dto = await service.UploadImageForStoreAsync(announcement.Id, new MemoryStream([1, 2, 3]), "a.png", "image/png");

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
        var service = CreateService(harness.Context, harness.Instructor, storeId: store.Id, fileStorage: fileStorage);

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.UploadImageForStoreAsync(target.Id, new MemoryStream([1]), "a.png", "image/png"));

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
        var service = CreateService(harness.Context, harness.Instructor, storeId: store.Id, fileStorage: fileStorage);

        await service.UploadImageForStoreAsync(announcement.Id, new MemoryStream([1, 2, 3]), "a.png", "image/png");
        await service.DeleteForStoreAsync(announcement.Id);

        Assert.Contains(fileStorage.SavedPaths[0], fileStorage.DeletedPaths);
    }

    private static StoreAnnouncementService CreateService(
        ENoteContext context,
        Instructor instructor,
        int storeId = 1,
        StubCurrentActor? actor = null,
        RecordingFileStorageService? fileStorage = null) =>
        new(context,
            new FixedClock(Now),
            actor ?? new StubCurrentActor(instructor: instructor, storeId: storeId),
            actor ?? new StubCurrentActor(instructor: instructor, storeId: storeId),
            TestMapper.Create(),
            fileStorage ?? new RecordingFileStorageService());
}
