using eNote.Application.Constants;
using eNote.Application.Features.Files.Services;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Assignments;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Files;

public sealed class FileAccessServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CanAccessAssignmentFileAsync_ReturnsFalse_WhenSubmissionMissing()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context, new StubUserIdentityService());

        Assert.False(await service.CanAccessAssignmentFileAsync(1, "missing.pdf"));
    }

    [Fact]
    public async Task CanAccessAssignmentFileAsync_Admin_CanAccessAnySubmission()
    {
        var harness = await SeedSubmissionAsync();
        var identity = new StubUserIdentityService(roles: new Dictionary<int, IReadOnlyList<string>>
        {
            [99] = [AppRoles.Administrator]
        });
        var service = CreateService(harness.Context, identity, harness.Instructor);

        Assert.True(await service.CanAccessAssignmentFileAsync(99, harness.FileName));
    }

    [Fact]
    public async Task CanAccessAssignmentFileAsync_Student_CanAccessOwnSubmission()
    {
        var harness = await SeedSubmissionAsync();
        var identity = new StubUserIdentityService(roles: new Dictionary<int, IReadOnlyList<string>>
        {
            [harness.Student.AppUserId] = [AppRoles.Student]
        });
        var service = CreateService(harness.Context, identity, harness.Instructor, harness.Student);

        Assert.True(await service.CanAccessAssignmentFileAsync(harness.Student.AppUserId, harness.FileName));
    }

    [Fact]
    public async Task CanAccessAssignmentFileAsync_Student_CannotAccessOtherSubmission()
    {
        var harness = await SeedSubmissionAsync();
        var identity = new StubUserIdentityService(roles: new Dictionary<int, IReadOnlyList<string>>
        {
            [77] = [AppRoles.Student]
        });
        var otherStudent = new Student(77, Now);
        var service = CreateService(harness.Context, identity, harness.Instructor, otherStudent);

        Assert.False(await service.CanAccessAssignmentFileAsync(77, harness.FileName));
    }

    [Fact]
    public async Task CanAccessAssignmentFileAsync_Instructor_CanAccessOwnedCourseSubmission()
    {
        var harness = await SeedSubmissionAsync();
        var identity = new StubUserIdentityService(roles: new Dictionary<int, IReadOnlyList<string>>
        {
            [harness.Instructor.AppUserId] = [AppRoles.Instructor]
        });
        var service = CreateService(harness.Context, identity, harness.Instructor);

        Assert.True(await service.CanAccessAssignmentFileAsync(harness.Instructor.AppUserId, harness.FileName));
    }

    [Fact]
    public async Task CanAccessAssignmentFileAsync_Instructor_CannotAccessOtherCourseSubmission()
    {
        var harness = await SeedSubmissionAsync();
        var otherInstructor = new Instructor(222);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        await harness.Context.SaveChangesAsync();
        var identity = new StubUserIdentityService(roles: new Dictionary<int, IReadOnlyList<string>>
        {
            [222] = [AppRoles.Instructor]
        });
        var service = CreateService(harness.Context, identity, otherInstructor);

        Assert.False(await service.CanAccessAssignmentFileAsync(222, harness.FileName));
    }

    private static FileAccessService CreateService(ENoteContext context, StubUserIdentityService identity, Instructor? instructor = null, Student? student = null) =>
        new(context,
            new StubUserProfileLookup(student: student, instructor: instructor),
            new InstructorAccessService(context, new StubUserProfileLookup(instructor: instructor)),
            identity);

    private static async Task<SubmissionHarness> SeedSubmissionAsync()
    {
        var context = TestDbContextFactory.CreateContext(Now);
        var instructor = new Instructor(100);
        context.Set<Instructor>().Add(instructor);
        await context.SaveChangesAsync();
        var course = new Course("Guitar", null, 100m, Now, null, instructor.Id);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();
        var lecture = new Lecture("Lecture", "Room", 60, Now, LectureType.Theoretical, null, course.Id);
        context.Set<Lecture>().Add(lecture);
        await context.SaveChangesAsync();
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), lecture.Id);
        context.Set<Assignment>().Add(assignment);
        await context.SaveChangesAsync();
        var student = new Student(50, Now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var submission = new AssignmentSubmission(assignment.Id, student.Id);
        submission.Submit($"/api/uploads/assignments/{Guid.NewGuid():N}.pdf", Now);
        context.Set<AssignmentSubmission>().Add(submission);
        await context.SaveChangesAsync();

        return new SubmissionHarness(context, instructor, student, Path.GetFileName(submission.FilePath)!);
    }

    private sealed record SubmissionHarness(ENoteContext Context, Instructor Instructor, Student Student, string FileName);
}
