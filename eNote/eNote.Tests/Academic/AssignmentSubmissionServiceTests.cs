using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Assignments.Services;
using eNote.Application.Features.Identity.Users;
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
        var submission = new AssignmentSubmission(assignment.Id, harness.Student.Id);
        submission.Submit("/hw.pdf", Now);
        harness.Context.Set<AssignmentSubmission>().Add(submission);
        await harness.Context.SaveChangesAsync();
        var fileStorage = new RecordingFileStorageService();
        var service = CreateService(harness.Context, harness.Instructor, fileStorage, harness.Student);
        using var stream = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.SubmitWithFileAsync(assignment.Id, stream, "hw2.pdf", "application/pdf"));

        Assert.Empty(fileStorage.SavedFiles);
    }

    [Fact]
    public async Task SubmitWithFileAsync_Throws_WhenPastDue()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(-1), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var fileStorage = new RecordingFileStorageService();
        var service = CreateService(harness.Context, harness.Instructor, fileStorage, harness.Student);
        using var stream = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<BusinessException>(() =>
            service.SubmitWithFileAsync(assignment.Id, stream, "hw.pdf", "application/pdf"));

        Assert.Empty(fileStorage.SavedFiles);
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
    public async Task GradeAsync_StoresFeedback()
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

        var dto = await service.GradeAsync(harness.Lecture.Id, assignment.Id, submission.Id, new GradeAssignmentRequest { Grade = 85, Feedback = "Odlično" });

        Assert.Equal(85, dto.Grade);
        Assert.Equal("Odlično", dto.Feedback);
        var row = await harness.Context.Set<AssignmentSubmission>().SingleAsync();
        Assert.Equal(85, row.Grade);
        Assert.Equal("Odlično", row.Feedback);
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
    public async Task GradeAsync_Throws_WhenSubmissionNotSubmitted()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var submission = new AssignmentSubmission(assignment.Id, harness.Student.Id);
        harness.Context.Set<AssignmentSubmission>().Add(submission);
        await harness.Context.SaveChangesAsync();
        var dispatcher = new RecordingSubmissionNotificationDispatcher();
        var service = CreateService(harness.Context, harness.Instructor, new RecordingFileStorageService(), harness.Student, dispatcher);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.GradeAsync(harness.Lecture.Id, assignment.Id, submission.Id, new GradeAssignmentRequest { Grade = 85 }));

        Assert.Equal(Messages.AssignmentNotSubmitted, ex.Message);
        Assert.Null(submission.Grade);
        Assert.Empty(dispatcher.GradedCalls);
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
        var fileStorage = new RecordingFileStorageService();
        var service = CreateService(context, harness.Context, harness.Instructor, fileStorage, harness.Student);
        using var stream = new MemoryStream([1, 2, 3]);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.SubmitWithFileAsync(assignment.Id, stream, "hw.pdf", "application/pdf"));

        Assert.Equal(Messages.AssignmentAlreadySubmitted, ex.Message);
        Assert.Single(fileStorage.SavedFiles);
        Assert.Single(fileStorage.DeletedPaths);
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

    [Fact]
    public async Task SubmitWithFileAsync_Throws_ForAssignmentOfClassmatesIndividualLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var classmate = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        var classmateLecture = await AcademicTestData.AddIndividualLectureAsync(harness, classmate, Now.AddDays(2));
        var assignment = new Assignment("Individual homework", "Do it", Now.AddDays(7), classmateLecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var fileStorage = new RecordingFileStorageService();
        var service = CreateService(harness.Context, harness.Instructor, fileStorage, harness.Student);
        using var stream = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.SubmitWithFileAsync(assignment.Id, stream, "hw.pdf", "application/pdf"));

        Assert.Empty(fileStorage.SavedFiles);
    }

    [Fact]
    public async Task SubmitWithFileAsync_CreatesSubmission_ForAssignmentOfOwnIndividualLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var ownLecture = await AcademicTestData.AddIndividualLectureAsync(harness, harness.Student, Now.AddDays(2));
        var assignment = new Assignment("Individual homework", "Do it", Now.AddDays(7), ownLecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, new RecordingFileStorageService(), harness.Student);
        using var stream = new MemoryStream([1, 2, 3]);

        var dto = await service.SubmitWithFileAsync(assignment.Id, stream, "hw.pdf", "application/pdf");

        Assert.NotNull(dto.SubmittedAt);
    }

    [Fact]
    public async Task GetSubmissionsAsync_FiltersByGradedState()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var classmate = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var graded = new AssignmentSubmission(assignment.Id, harness.Student.Id);
        graded.Submit("/hw1.pdf", Now);
        graded.SetGrade(90);
        var ungraded = new AssignmentSubmission(assignment.Id, classmate.Id);
        ungraded.Submit("/hw2.pdf", Now);
        harness.Context.Set<AssignmentSubmission>().AddRange(graded, ungraded);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, new RecordingFileStorageService(), harness.Student);

        var gradedResult = await service.GetSubmissionsAsync(harness.Lecture.Id, assignment.Id, new SubmissionSearchObject { IsGraded = true });
        var ungradedResult = await service.GetSubmissionsAsync(harness.Lecture.Id, assignment.Id, new SubmissionSearchObject { IsGraded = false });

        Assert.Equal(graded.Id, Assert.Single(gradedResult.Items).Id);
        Assert.Equal(ungraded.Id, Assert.Single(ungradedResult.Items).Id);
    }

    [Fact]
    public async Task GetSubmissionsAsync_FiltersByStudentName()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var classmate = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var ownSubmission = new AssignmentSubmission(assignment.Id, harness.Student.Id);
        ownSubmission.Submit("/hw1.pdf", Now);
        var classmateSubmission = new AssignmentSubmission(assignment.Id, classmate.Id);
        classmateSubmission.Submit("/hw2.pdf", Now);
        harness.Context.Set<AssignmentSubmission>().AddRange(ownSubmission, classmateSubmission);
        await harness.Context.SaveChangesAsync();
        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [harness.Student.AppUserId] = StubUserIdentityService.User(harness.Student.AppUserId, "jdoe", "Jane", "Doe"),
            [classmate.AppUserId] = StubUserIdentityService.User(classmate.AppUserId, "asmith", "Ann", "Smith")
        });
        var service = CreateService(harness.Context, harness.Instructor, new RecordingFileStorageService(), harness.Student, identity: identity);

        var result = await service.GetSubmissionsAsync(harness.Lecture.Id, assignment.Id, new SubmissionSearchObject { StudentName = "Jane" });

        Assert.Equal(ownSubmission.Id, Assert.Single(result.Items).Id);
    }

    private static AssignmentSubmissionService CreateService(IAppDbContext context, Instructor instructor, IFileStorageService fileStorage, Student student, ISubmissionNotificationDispatcher? notificationDispatcher = null, IUserIdentityService? identity = null) =>
        CreateService(context, (ENoteContext)context, instructor, fileStorage, student, notificationDispatcher, identity);

    private static AssignmentSubmissionService CreateService(IAppDbContext context, ENoteContext accessContext, Instructor instructor, IFileStorageService fileStorage, Student student, ISubmissionNotificationDispatcher? notificationDispatcher = null, IUserIdentityService? identity = null)
    {
        var currentUser = new StubCurrentActor(student: student);
        return new(context,
            new FixedClock(Now),
            currentUser,
            currentUser,
            new StubDisplayNameService(),
            identity ?? new StubUserIdentityService(),
            AcademicTestData.CreateInstructorAccess(accessContext, instructor),
            fileStorage,
            notificationDispatcher ?? new NoOpSubmissionNotificationDispatcher(),
            TestMapper.Create());
    }
}
