using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Communication.Announcements.Services;
using eNote.Application.Features.Identity.Instructors;
using eNote.Domain.Entities.Communication;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Communication;

public sealed class AnnouncementServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateForCourseAsync_Throws_WhenInstructorDoesNotOwnCourse()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherInstructor = new Instructor(200);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, new InstructorAccessService(harness.Context, new StubUserProfileLookup(instructor: otherInstructor)));

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateForCourseAsync(harness.Course.Id, new AnnouncementRequest("Title", "Content")));
    }

    [Fact]
    public async Task CreateForCourseAsync_Succeeds_ForCourseOwner()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor, AcademicTestData.CreateInstructorAccess(harness.Context, harness.Instructor));

        var dto = await service.CreateForCourseAsync(harness.Course.Id, new AnnouncementRequest("Welcome", "Hello everyone"));

        Assert.Equal("Welcome", dto.Title);
        Assert.Equal(harness.Course.Id, dto.CourseId);
        var row = await harness.Context.Set<Announcement>().SingleAsync();
        Assert.Equal(Now, row.PublishedAt);
    }

    [Fact]
    public async Task CreateForStoreAsync_AddsStoreAnnouncement()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor, AcademicTestData.CreateInstructorAccess(harness.Context, harness.Instructor), storeId: 3);

        var dto = await service.CreateForStoreAsync(new AnnouncementRequest("Sale", "20% off"));

        Assert.Equal("Sale", dto.Title);
        var row = await harness.Context.Set<Announcement>().AsNoTracking().IgnoreQueryFilters().SingleAsync(a => a.Title == "Sale");
        Assert.Equal(3, row.MusicStoreId);
    }

    [Fact]
    public async Task GetFeedForStudentAsync_IncludesCourseAndStoreAnnouncements()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var store = new MusicStore("Music Shop", "09-17");
        harness.Context.Set<MusicStore>().Add(store);
        await harness.Context.SaveChangesAsync();
        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        harness.Context.Set<InstrumentType>().Add(type);
        await harness.Context.SaveChangesAsync();
        var instrument = new Instrument("Strat", "Fender", null, null, type.Id, store.Id);
        harness.Context.Set<Instrument>().Add(instrument);
        await harness.Context.SaveChangesAsync();
        var rental = new InstrumentRental(instrument.Id, harness.Student.Id, store.Id, Now, null);
        rental.Approve(50m, null, Now, 1);
        harness.Context.Set<InstrumentRental>().Add(rental);
        await harness.Context.SaveChangesAsync();

        harness.Context.Set<Announcement>().AddRange(
            new Announcement("Course note", "For the course", harness.Course.Id, null, Now),
            new Announcement("Store note", "For the store", null, store.Id, Now),
            new Announcement("Other course", "Not enrolled", 999, null, Now));
        await harness.Context.SaveChangesAsync();

        var actor = new StubCurrentActor(student: harness.Student);
        var service = CreateService(harness.Context, harness.Instructor, AcademicTestData.CreateInstructorAccess(harness.Context, harness.Instructor), actor: actor);

        var result = await service.GetFeedForStudentAsync(new AnnouncementSearchObject { Page = 1, PageSize = 10 });

        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, a => a.Title == "Course note");
        Assert.Contains(result.Items, a => a.Title == "Store note");
    }

    [Fact]
    public async Task DeleteForCourseAsync_SoftDeletesAnnouncement()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var announcement = new Announcement("Welcome", "Hello", harness.Course.Id, null, Now);
        harness.Context.Set<Announcement>().Add(announcement);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, AcademicTestData.CreateInstructorAccess(harness.Context, harness.Instructor));

        await service.DeleteForCourseAsync(harness.Course.Id, announcement.Id);

        var row = await harness.Context.Set<Announcement>().AsNoTracking().IgnoreQueryFilters().SingleAsync();
        Assert.False(row.IsActive);
    }

    private static AnnouncementService CreateService(
        ENoteContext context,
        Instructor instructor,
        InstructorAccessService instructorAccess,
        int storeId = 1,
        StubCurrentActor? actor = null) =>
        new(context,
            new FixedClock(Now),
            actor ?? new StubCurrentActor(instructor: instructor, storeId: storeId),
            instructorAccess,
            new RecordingFileStorageService(),
            TestMapper.Create());
}
