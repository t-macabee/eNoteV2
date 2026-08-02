using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Academic;

public sealed class CourseServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_CreatesCourse_ForCurrentInstructor()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);
        var request = new CourseRequest { Name = "Piano", Price = 150m, StartDate = Now, EndDate = Now.AddMonths(3), IsPublished = false };

        var dto = await service.CreateAsync(request);

        Assert.Equal("Piano", dto.Name);
        var row = await harness.Context.Set<Course>().SingleAsync(c => c.Name == "Piano");
        Assert.Equal(harness.Instructor.Id, row.InstructorId);
    }

    [Fact]
    public async Task GetByIdForInstructorAsync_Throws_WhenCourseBelongsToAnotherInstructor()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherInstructor = new Instructor(200);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, otherInstructor);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForInstructorAsync(harness.Course.Id));
    }

    [Fact]
    public async Task GetByIdForStudentAsync_ReturnsCourse_WhenEnrolled()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var actor = new StubCurrentActor(student: harness.Student);
        var service = CreateService(harness.Context, harness.Instructor, actor);

        var dto = await service.GetByIdForStudentAsync(harness.Course.Id);

        Assert.Equal(harness.Course.Id, dto.Id);
    }

    [Fact]
    public async Task GetByIdForStudentAsync_Throws_WhenCourseUnpublishedAndNotEnrolled()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var course = new Course("Private", null, 100m, Now, null, harness.Instructor.Id);
        course.SetPublishedStatus(false);
        harness.Context.Set<Course>().Add(course);
        await harness.Context.SaveChangesAsync();
        var actor = new StubCurrentActor(student: new Student(77, Now));
        var service = CreateService(harness.Context, harness.Instructor, actor);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForStudentAsync(course.Id));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOwnedCourse()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);
        var request = new CourseRequest { Name = "Renamed", Price = 200m, StartDate = Now, EndDate = Now.AddMonths(2), IsPublished = true };

        var dto = await service.UpdateAsync(harness.Course.Id, request);

        Assert.Equal("Renamed", dto.Name);
        Assert.True(dto.IsPublished);
    }

    private static CourseService CreateService(ENoteContext context, Instructor instructor, StubCurrentActor? actor = null) =>
        new(context,
            TestMapper.Create(),
            actor ?? new StubCurrentActor(instructor: instructor),
            AcademicTestData.CreateInstructorAccess(context, instructor),
            NullLogger<CourseService>.Instance);
}
