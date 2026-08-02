using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Assignments;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Identity;

public sealed class InstructorAccessServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetCurrentInstructorIdAsync_UsesLookup()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var instructor = new Instructor(100);
        context.Set<Instructor>().Add(instructor);
        await context.SaveChangesAsync();
        var service = CreateService(context, instructor);

        Assert.Equal(instructor.Id, await service.GetCurrentInstructorIdAsync(100));
    }

    [Fact]
    public async Task OwnsCourseAsync_ReturnsTrue_ForOwnedCourse()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var instructor = await SeedInstructorAsync(context);
        var course = new Course("Guitar", null, 100m, Now, null, instructor.Id);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();
        var service = CreateService(context, instructor);

        Assert.True(await service.OwnsCourseAsync(course.Id, instructor.Id));
        Assert.False(await service.OwnsCourseAsync(course.Id, instructor.Id + 100));
    }

    [Fact]
    public async Task EnsureOwnsLectureAsync_Throws_WhenLectureNotOwned()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var instructor = await SeedInstructorAsync(context);
        var course = new Course("Guitar", null, 100m, Now, null, instructor.Id);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();
        context.Set<Lecture>().Add(new Lecture("Lecture", "Room", 60, Now, LectureType.Theoretical, null, course.Id));
        await context.SaveChangesAsync();
        var otherInstructor = new Instructor(200);
        context.Set<Instructor>().Add(otherInstructor);
        await context.SaveChangesAsync();
        var service = CreateService(context, instructor);

        await Assert.ThrowsAsync<AuthorizationException>(() => service.EnsureOwnsLectureAsync(1, otherInstructor.Id));
    }

    [Fact]
    public async Task GetOwnedAssignmentAsync_Throws_WhenAssignmentNotOwned()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var instructor = await SeedInstructorAsync(context);
        var course = new Course("Guitar", null, 100m, Now, null, instructor.Id);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();
        context.Set<Lecture>().Add(new Lecture("Lecture", "Room", 60, Now, LectureType.Theoretical, null, course.Id));
        await context.SaveChangesAsync();
        context.Set<Assignment>().Add(new Assignment("Homework", "Do it", Now.AddDays(7), 1));
        await context.SaveChangesAsync();
        var otherInstructor = new Instructor(300);
        context.Set<Instructor>().Add(otherInstructor);
        await context.SaveChangesAsync();
        var service = CreateService(context, instructor);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetOwnedAssignmentAsync(1, 1, otherInstructor.Id));
    }

    [Fact]
    public async Task GetOwnedAssignmentAsync_ReturnsAssignment_ForOwnedCourse()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var instructor = await SeedInstructorAsync(context);
        var course = new Course("Guitar", null, 100m, Now, null, instructor.Id);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();
        context.Set<Lecture>().Add(new Lecture("Lecture", "Room", 60, Now, LectureType.Theoretical, null, course.Id));
        await context.SaveChangesAsync();
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), 1);
        context.Set<Assignment>().Add(assignment);
        await context.SaveChangesAsync();
        var service = CreateService(context, instructor);

        var result = await service.GetOwnedAssignmentAsync(1, assignment.Id, instructor.Id);

        Assert.Equal(assignment.Id, result.Id);
    }

    private static InstructorAccessService CreateService(ENoteContext context, Instructor instructor) =>
        new(context, new StubUserProfileLookup(instructor: instructor));

    private static async Task<Instructor> SeedInstructorAsync(ENoteContext context)
    {
        var instructor = new Instructor(100);
        context.Set<Instructor>().Add(instructor);
        await context.SaveChangesAsync();
        return instructor;
    }
}
