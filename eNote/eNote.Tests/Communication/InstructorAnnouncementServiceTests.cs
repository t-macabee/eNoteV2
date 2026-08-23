using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Communication.Announcements.Services;
using eNote.Application.Features.Identity.Instructors;
using eNote.Domain.Entities.Communication;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Communication;

public sealed class InstructorAnnouncementServiceTests
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

    private static InstructorAnnouncementService CreateService(
        ENoteContext context,
        Instructor instructor,
        InstructorAccessService instructorAccess,
        StubCurrentActor? actor = null) =>
        new(context,
            new FixedClock(Now),
            actor ?? new StubCurrentActor(instructor: instructor),
            instructorAccess,
            new RecordingFileStorageService(),
            TestMapper.Create());
}
