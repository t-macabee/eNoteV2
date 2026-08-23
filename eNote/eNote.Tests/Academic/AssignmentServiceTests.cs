using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Assignments.Services;
using eNote.Domain.Entities.Assignments;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Academic;

public sealed class AssignmentServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_CreatesAssignment_ForOwnedLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.CreateAsync(harness.Lecture.Id, new AssignmentRequest
        {
            Title = "Homework 1",
            Description = "Do the exercises",
            DueAt = Now.AddDays(7)
        });

        Assert.Equal("Homework 1", dto.Title);
        Assert.Equal(harness.Lecture.Id, dto.LectureId);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenLectureNotOwned()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherInstructor = new Instructor(300);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, otherInstructor);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            service.CreateAsync(harness.Lecture.Id, new AssignmentRequest
            {
                Title = "Homework",
                Description = "Desc",
                DueAt = Now.AddDays(7)
            }));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOwnedAssignment()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Old", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor);

        var dto = await service.UpdateAsync(harness.Lecture.Id, assignment.Id, new AssignmentRequest
        {
            Title = "Homework v2",
            Description = "New",
            DueAt = Now.AddDays(14)
        });

        Assert.Equal("Homework v2", dto.Title);
        Assert.Equal(Now.AddDays(14), dto.DueAt);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesAssignment()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Old", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor);

        await service.DeleteAsync(harness.Lecture.Id, assignment.Id);

        var row = await harness.Context.Set<Assignment>().AsNoTracking().IgnoreQueryFilters().SingleAsync();
        Assert.False(row.IsActive);
    }

    [Fact]
    public async Task GetForStudentAsync_ReturnsOnlyEnrolledStudentsAssignments()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        harness.Context.Set<Assignment>().Add(new Assignment("Hidden", "Not for you", Now.AddDays(7), 9999));
        await harness.Context.SaveChangesAsync();
        var actor = new StubCurrentActor(student: harness.Student);
        var service = CreateService(harness.Context, harness.Instructor, actor);

        var result = await service.GetForStudentAsync(new AssignmentSearchObject { Page = 1, PageSize = 10 });

        var dto = Assert.Single(result.Items);
        Assert.Equal("Homework", dto.Title);
    }

    private static AssignmentService CreateService(ENoteContext context, Instructor instructor, StubCurrentActor? actor = null)
    {
        var currentUser = actor ?? new StubCurrentActor(instructor: instructor);
        return new(context,
            currentUser,
            currentUser,
            AcademicTestData.CreateInstructorAccess(context, instructor),
            TestMapper.Create());
    }
}
