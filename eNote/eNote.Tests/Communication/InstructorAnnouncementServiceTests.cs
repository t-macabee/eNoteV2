using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Communication.Announcements.Services;
using eNote.Application.Features.Identity.Instructors;
using eNote.Contracts.Communication;
using eNote.Domain.Entities.Academic;
using eNote.Domain.Entities.Communication;
using eNote.Domain.Enums;
using eNote.Infrastructure.Messaging;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace eNote.Tests.Communication;

public sealed class InstructorAnnouncementServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

        Assert.Empty(await harness.Context.Set<NotificationOutbox>().ToListAsync());
    }

    [Fact]
    public async Task CreateForCourseAsync_EnqueuesOneMessagePerActiveStudent()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var droppedStudent = new Student(60, Now);
        harness.Context.Set<Student>().Add(droppedStudent);
        await harness.Context.SaveChangesAsync();
        harness.Context.Set<Enrollment>().Add(new Enrollment(droppedStudent.Id, harness.Course.Id, EnrollmentStatus.Canceled));
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, AcademicTestData.CreateInstructorAccess(harness.Context, harness.Instructor));

        var dto = await service.CreateForCourseAsync(harness.Course.Id, new AnnouncementRequest("Welcome", "Hello everyone"));

        var outbox = Assert.Single(await harness.Context.Set<NotificationOutbox>().ToListAsync());
        Assert.Equal(NotificationMessageTypes.AnnouncementPublished, outbox.MessageType);
        var payload = JsonSerializer.Deserialize<AnnouncementPublished>(outbox.PayloadJson, JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(dto.Id, payload.AnnouncementId);
        Assert.NotEqual(0, payload.AnnouncementId);
        Assert.Equal(harness.Student.AppUserId, payload.StudentUserId);
        Assert.Equal("Welcome", payload.Body);
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

    [Fact]
    public async Task GetForCourseAsync_FiltersByTitle()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Announcement>().AddRange(
            new Announcement("Exam schedule", "Hello", harness.Course.Id, null, Now),
            new Announcement("Welcome", "Hello", harness.Course.Id, null, Now));
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, AcademicTestData.CreateInstructorAccess(harness.Context, harness.Instructor));

        var result = await service.GetForCourseAsync(harness.Course.Id, new AnnouncementSearchObject { Title = "Exam", Page = 1, PageSize = 10 });

        Assert.Equal("Exam schedule", Assert.Single(result.Items).Title);
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
            TestMapper.Create(),
            new AnnouncementNotificationDispatcher(context, new FixedClock(Now)));
}
