using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
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
    public async Task GradeAsync_DispatchesGradedNotification_ForSubmittingStudent()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var submission = new AssignmentSubmission(assignment.Id, harness.Student.Id);
        submission.Submit("/api/uploads/assignments/hw.pdf", Now);
        harness.Context.Set<AssignmentSubmission>().Add(submission);
        await harness.Context.SaveChangesAsync();
        var dispatcher = new RecordingSubmissionNotificationDispatcher();
        var service = CreateService(harness.Context, harness.Instructor, new RecordingFileStorageService(), harness.Student, dispatcher);

        await service.GradeAsync(harness.Lecture.Id, assignment.Id, submission.Id, new GradeAssignmentRequest { Grade = 85 });

        var call = Assert.Single(dispatcher.GradedCalls);
        Assert.Equal(submission.Id, call.SubmissionId);
        Assert.Equal(harness.Student.AppUserId, call.StudentUserId);
        Assert.Equal("Homework", call.AssignmentTitle);
        Assert.Equal(85, call.Grade);
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

    [Fact]
    public async Task SubmitWithFileAsync_TranslatesUniqueIndexViolation_ToConflict()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.AssignmentSubmissionAssignmentIdStudentIdUniqueIndex}\"");
        var context = new ThrowingSaveDbContext(harness.Context, new DbUpdateException("Unique constraint violated.", inner));
        var service = CreateService(context, harness.Context, harness.Instructor, new RecordingFileStorageService(), harness.Student);
        using var stream = new MemoryStream([1, 2, 3]);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.SubmitWithFileAsync(assignment.Id, stream, "hw.pdf", "application/pdf"));

        Assert.Equal(Messages.AssignmentAlreadySubmitted, ex.Message);
    }

    [Fact]
    public async Task InMemoryProvider_AcceptsDuplicateSubmissions_Characterization()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var first = new AssignmentSubmission(assignment.Id, harness.Student.Id);
        first.Submit("/hw1.pdf", Now);
        var second = new AssignmentSubmission(assignment.Id, harness.Student.Id);
        second.Submit("/hw2.pdf", Now);
        harness.Context.Set<AssignmentSubmission>().Add(first);
        harness.Context.Set<AssignmentSubmission>().Add(second);

        await harness.Context.SaveChangesAsync();

        Assert.Equal(2, await harness.Context.Set<AssignmentSubmission>().CountAsync());
    }

    private static AssignmentSubmissionService CreateService(IAppDbContext context, Instructor instructor, IFileStorageService fileStorage, Student student, ISubmissionNotificationDispatcher? notificationDispatcher = null) =>
        CreateService(context, (ENoteContext)context, instructor, fileStorage, student, notificationDispatcher);

    private static AssignmentSubmissionService CreateService(IAppDbContext context, ENoteContext accessContext, Instructor instructor, IFileStorageService fileStorage, Student student, ISubmissionNotificationDispatcher? notificationDispatcher = null) =>
        new(context,
            new FixedClock(Now),
            new StubCurrentActor(student: student),
            new StubDisplayNameService(),
            AcademicTestData.CreateInstructorAccess(accessContext, instructor),
            fileStorage,
            notificationDispatcher ?? new NoOpSubmissionNotificationDispatcher(),
            TestMapper.Create());

    private sealed class StubDisplayNameService : IStudentDisplayNameService
    {
        public Task<string> GetStudentDisplayNameAsync(Student student) => Task.FromResult($"Student {student.Id}");
        public Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students) =>
            Task.FromResult<IReadOnlyDictionary<int, string>>(students.ToDictionary(s => s.Id, s => $"Student {s.Id}"));
    }
}
