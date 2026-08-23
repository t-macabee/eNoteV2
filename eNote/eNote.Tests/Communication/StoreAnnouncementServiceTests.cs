using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Communication.Announcements.Services;
using eNote.Application.Features.Identity.Instructors;
using eNote.Domain.Entities.Communication;
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

    private static StoreAnnouncementService CreateService(
        ENoteContext context,
        Instructor instructor,
        int storeId = 1,
        StubCurrentActor? actor = null) =>
        new(context,
            new FixedClock(Now),
            actor ?? new StubCurrentActor(instructor: instructor, storeId: storeId),
            actor ?? new StubCurrentActor(instructor: instructor, storeId: storeId),
            new RecordingFileStorageService(),
            TestMapper.Create());
}
