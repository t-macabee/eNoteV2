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

    private static StoreAnnouncementService CreateService(
        ENoteContext context,
        Instructor instructor,
        int storeId = 1,
        StubCurrentActor? actor = null) =>
        new(context,
            new FixedClock(Now),
            actor ?? new StubCurrentActor(instructor: instructor, storeId: storeId),
            actor ?? new StubCurrentActor(instructor: instructor, storeId: storeId),
            TestMapper.Create());
}
