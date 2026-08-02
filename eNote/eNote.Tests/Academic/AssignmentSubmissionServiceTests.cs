using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Assignments.Services;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Assignments;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Academic;

public sealed class AssignmentSubmissionServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SubmitWithFileAsync_SavesFile_AndSubmits()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var fileStorage = new RecordingFileStorageService();
        var service = CreateService(harness.Context, harness.Instructor, fileStorage, harness.Student);
        using var stream = new MemoryStream([1, 2, 3]);

        var dto = await service.SubmitWithFileAsync(assignment.Id, stream, "hw.pdf", "application/pdf");

        Assert.NotNull(dto.SubmittedAt);
        Assert.Single(fileStorage.SavedFiles);
        var submission = await harness.Context.Set<AssignmentSubmission>().SingleAsync();
        Assert.Equal(harness.Student.Id, submission.StudentId);
        Assert.Equal(assignment.Id, submission.AssignmentId);
    }

    [Fact]
    public async Task SubmitWithFileAsync_Throws_WhenAlreadySubmitted()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, new RecordingFileStorageService(), harness.Student);
        using var stream = new MemoryStream([1, 2, 3]);
        await service.SubmitWithFileAsync(assignment.Id, stream, "hw.pdf", "application/pdf");

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.SubmitWithFileAsync(assignment.Id, stream, "hw2.pdf", "application/pdf"));
    }

    [Fact]
    public async Task SubmitWithFileAsync_Throws_WhenPastDue()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(-1), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, new RecordingFileStorageService(), harness.Student);
        using var stream = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.SubmitWithFileAsync(assignment.Id, stream, "hw.pdf", "application/pdf"));
    }

    [Fact]
    public async Task GradeAsync_GradesSubmission_ForOwnedAssignment()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var submission = new AssignmentSubmission(assignment.Id, harness.Student.Id);
        submission.Submit("/api/uploads/assignments/hw.pdf", Now);
        harness.Context.Set<AssignmentSubmission>().Add(submission);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, new RecordingFileStorageService(), harness.Student);

        var dto = await service.GradeAsync(harness.Lecture.Id, assignment.Id, submission.Id, new GradeAssignmentRequest { Grade = 85 });

        Assert.Equal(85, dto.Grade);
        var row = await harness.Context.Set<AssignmentSubmission>().SingleAsync();
        Assert.Equal(85, row.Grade);
    }

    [Fact]
    public async Task GradeAsync_Throws_WhenInstructorDoesNotOwnAssignment()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var submission = new AssignmentSubmission(assignment.Id, harness.Student.Id);
        submission.Submit("/api/uploads/assignments/hw.pdf", Now);
        harness.Context.Set<AssignmentSubmission>().Add(submission);
        await harness.Context.SaveChangesAsync();
        var otherInstructor = new Instructor(400);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, otherInstructor, new RecordingFileStorageService(), harness.Student);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GradeAsync(harness.Lecture.Id, assignment.Id, submission.Id, new GradeAssignmentRequest { Grade = 85 }));
    }

    private static AssignmentSubmissionService CreateService(ENoteContext context, Instructor instructor, IFileStorageService fileStorage, Student student) =>
        new(context,
            new FixedClock(Now),
            new StubCurrentActor(student: student),
            new StubDisplayNameService(),
            AcademicTestData.CreateInstructorAccess(context, instructor),
            fileStorage,
            TestMapper.Create());

    private sealed class StubDisplayNameService : IStudentDisplayNameService
    {
        public Task<string> GetStudentDisplayNameAsync(Student student) => Task.FromResult($"Student {student.Id}");
        public Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students) =>
            Task.FromResult<IReadOnlyDictionary<int, string>>(students.ToDictionary(s => s.Id, s => $"Student {s.Id}"));
    }
}
