using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Tests.TestUtils;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Academic;

public sealed class LectureServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_CreatesLecture_ForOwnedCourse()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.CreateAsync(new LectureCreateRequest
        {
            CourseId = harness.Course.Id,
            Name = "Second lesson",
            Location = "Room 2",
            Duration = 90,
            LectureTime = Now.AddDays(1),
            LectureType = LectureType.Practical,
            Capacity = 20
        });

        Assert.Equal("Second lesson", dto.Name);
        Assert.Equal(LectureStatus.Scheduled, dto.LectureStatus);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenCourseNotOwned()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherInstructor = new Instructor(300);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, otherInstructor);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            service.CreateAsync(new LectureCreateRequest
            {
                CourseId = harness.Course.Id,
                Name = "Lesson",
                Location = "Room",
                Duration = 60,
                LectureTime = Now.AddDays(1),
                LectureType = LectureType.Theoretical
            }));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOwnedLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.UpdateAsync(harness.Lecture.Id, new LectureUpdateRequest
        {
            Name = "Renamed lesson",
            Location = "Lab",
            Duration = 120,
            LectureTime = Now.AddDays(2),
            Capacity = 10
        });

        Assert.Equal("Renamed lesson", dto.Name);
        Assert.Equal(120, dto.Duration);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenLectureCancelled()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<Lecture>().Single(l => l.Id == harness.Lecture.Id).Cancel();
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor);

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.UpdateAsync(harness.Lecture.Id, new LectureUpdateRequest
            {
                Name = "Renamed",
                Location = "Lab",
                Duration = 60,
                LectureTime = Now.AddDays(2)
            }));
    }

    [Fact]
    public async Task CancelAsync_CancelsOwnedLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.CancelAsync(harness.Lecture.Id);

        Assert.True(dto.IsCancelled);
    }

    [Fact]
    public async Task GetByIdForStudentAsync_Throws_WhenNotEnrolled()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var stranger = new Student(77, Now);
        harness.Context.Set<Student>().Add(stranger);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, new StubCurrentActor(student: stranger));

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForStudentAsync(harness.Lecture.Id));
    }

    private static LectureService CreateService(ENoteContext context, Instructor instructor, StubCurrentActor? actor = null) =>
        new(context,
            actor ?? new StubCurrentActor(instructor: instructor),
            AcademicTestData.CreateInstructorAccess(context, instructor),
            NullLogger<LectureService>.Instance,
            TestMapper.Create());
}
