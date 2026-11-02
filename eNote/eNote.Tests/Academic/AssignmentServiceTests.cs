using eNote.Application.Common.Localization;
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
        var service = AcademicTestData.CreateService<AssignmentService>(harness.Context, harness.Instructor);

        var dto = await service.CreateAsync(harness.Lecture.Id, new AssignmentCreateRequest
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
        var service = AcademicTestData.CreateService<AssignmentService>(harness.Context, otherInstructor);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            service.CreateAsync(harness.Lecture.Id, new AssignmentCreateRequest
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
        var service = AcademicTestData.CreateService<AssignmentService>(harness.Context, harness.Instructor);

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
        var service = AcademicTestData.CreateService<AssignmentService>(harness.Context, harness.Instructor);

        await service.DeleteAsync(harness.Lecture.Id, assignment.Id);

        var row = await harness.Context.Set<Assignment>().AsNoTracking().IgnoreQueryFilters().SingleAsync();
        Assert.False(row.IsActive);
    }

    [Fact]
    public async Task GetByIdForInstructorAsync_Throws_WhenAssignmentBelongsToAnotherLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherLecture = new Lecture("Second lesson", "Room 2", 60, Now.AddDays(1), LectureType.Theoretical, 30, harness.Course.Id)
        {
            CreatedById = harness.Instructor.AppUserId
        };
        harness.Context.Set<Lecture>().Add(otherLecture);
        await harness.Context.SaveChangesAsync();
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), otherLecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        var service = AcademicTestData.CreateService<AssignmentService>(harness.Context, harness.Instructor);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForInstructorAsync(harness.Lecture.Id, assignment.Id));
        Assert.Equal(Messages.AssignmentNotFound, ex.Message);
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
        var service = AcademicTestData.CreateService<AssignmentService>(harness.Context, harness.Instructor, actor);

        var result = await service.GetForStudentAsync(new AssignmentSearchObject { Page = 1, PageSize = 10 });

        var dto = Assert.Single(result.Items);
        Assert.Equal("Homework", dto.Title);
    }

    [Fact]
    public async Task GetForStudentAsync_ReturnsAssignments_AfterCourseUnpublished_WhenEnrolledAndPaid()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        harness.Context.Set<Course>().Single(c => c.Id == harness.Course.Id).SetPublishedStatus(false);
        await harness.Context.SaveChangesAsync();
        var actor = new StubCurrentActor(student: harness.Student);
        var service = AcademicTestData.CreateService<AssignmentService>(harness.Context, harness.Instructor, actor);

        var result = await service.GetForStudentAsync(new AssignmentSearchObject { Page = 1, PageSize = 10 });

        var dto = Assert.Single(result.Items);
        Assert.Equal("Homework", dto.Title);
    }

    [Fact]
    public async Task GetForStudentAsync_ReturnsAssignmentsOfCancelledLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        harness.Context.Set<Lecture>().Single(l => l.Id == harness.Lecture.Id).Cancel("Instrument je pokvaren", 1, Now);
        await harness.Context.SaveChangesAsync();
        var actor = new StubCurrentActor(student: harness.Student);
        var service = AcademicTestData.CreateService<AssignmentService>(harness.Context, harness.Instructor, actor);

        var result = await service.GetForStudentAsync(new AssignmentSearchObject { Page = 1, PageSize = 10 });

        Assert.Equal(assignment.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task GetForStudentAsync_PagesTiedDueAt_WithoutDuplicateOrGap()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var dueAt = Now.AddDays(7);
        var third = new Assignment("Homework 3", "Do it", dueAt, harness.Lecture.Id) { Id = 3 };
        var first = new Assignment("Homework 1", "Do it", dueAt, harness.Lecture.Id) { Id = 1 };
        var second = new Assignment("Homework 2", "Do it", dueAt, harness.Lecture.Id) { Id = 2 };
        harness.Context.Set<Assignment>().Add(third);
        await harness.Context.SaveChangesAsync();
        harness.Context.Set<Assignment>().Add(first);
        await harness.Context.SaveChangesAsync();
        harness.Context.Set<Assignment>().Add(second);
        await harness.Context.SaveChangesAsync();
        var actor = new StubCurrentActor(student: harness.Student);
        var service = AcademicTestData.CreateService<AssignmentService>(harness.Context, harness.Instructor, actor);

        var page1 = await service.GetForStudentAsync(new AssignmentSearchObject { Page = 1, PageSize = 2 });
        var page2 = await service.GetForStudentAsync(new AssignmentSearchObject { Page = 2, PageSize = 2 });

        var ids = page1.Items.Select(a => a.Id).Concat(page2.Items.Select(a => a.Id)).ToList();
        Assert.Equal(new[] { 1, 2, 3 }, ids);
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    [Fact]
    public async Task GetForStudentAsync_SetsMySubmittedAtAndMyGrade_FromOwnSubmission()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        await SubmitAsync(harness, assignment, harness.Student, grade: 85);
        var service = AcademicTestData.CreateService<AssignmentService>(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var result = await service.GetForStudentAsync(new AssignmentSearchObject { Page = 1, PageSize = 10 });

        var dto = Assert.Single(result.Items);
        Assert.Equal(Now.AddDays(-1), dto.MySubmittedAt);
        Assert.Equal(85, dto.MyGrade);
    }

    [Fact]
    public async Task GetForStudentAsync_DoesNotUse_AnotherStudentsSubmission()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var classmate = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        await SubmitAsync(harness, assignment, classmate, grade: 40);
        var service = AcademicTestData.CreateService<AssignmentService>(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var result = await service.GetForStudentAsync(new AssignmentSearchObject { Page = 1, PageSize = 10 });

        var dto = Assert.Single(result.Items);
        Assert.Null(dto.MySubmittedAt);
        Assert.Null(dto.MyGrade);
    }

    [Fact]
    public async Task GetForStudentAsync_WithIsSubmittedFalse_ReturnsOnlyOpenAssignments()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var open = new Assignment("Open", "Do it", Now.AddDays(7), harness.Lecture.Id);
        var submitted = new Assignment("Submitted", "Do it", Now.AddDays(8), harness.Lecture.Id);
        harness.Context.Set<Assignment>().AddRange(open, submitted);
        await harness.Context.SaveChangesAsync();
        await SubmitAsync(harness, submitted, harness.Student);
        var service = AcademicTestData.CreateService<AssignmentService>(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var result = await service.GetForStudentAsync(new AssignmentSearchObject { IsSubmitted = false, Page = 1, PageSize = 10 });

        Assert.Equal(open.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task GetForStudentAsync_WithIsSubmittedTrue_ReturnsOnlySubmittedAssignments()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var open = new Assignment("Open", "Do it", Now.AddDays(7), harness.Lecture.Id);
        var submitted = new Assignment("Submitted", "Do it", Now.AddDays(8), harness.Lecture.Id);
        harness.Context.Set<Assignment>().AddRange(open, submitted);
        await harness.Context.SaveChangesAsync();
        await SubmitAsync(harness, submitted, harness.Student);
        var service = AcademicTestData.CreateService<AssignmentService>(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var result = await service.GetForStudentAsync(new AssignmentSearchObject { IsSubmitted = true, Page = 1, PageSize = 10 });

        Assert.Equal(submitted.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task GetByIdForStudentAsync_SetsMyGrade()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        await SubmitAsync(harness, assignment, harness.Student, grade: 70);
        var service = AcademicTestData.CreateService<AssignmentService>(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var dto = await service.GetByIdForStudentAsync(assignment.Id);

        Assert.Equal(70, dto.MyGrade);
    }

    [Fact]
    public async Task GetForLectureAsync_ReturnsEmpty_ForOtherInstructorsLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var other = await AcademicTestData.AddSecondInstructorContentAsync(harness, Now);
        var service = AcademicTestData.CreateService<AssignmentService>(harness.Context, harness.Instructor);

        var result = await service.GetForLectureAsync(other.Lecture.Id, new AssignmentSearchObject { Page = 1, PageSize = 10 });

        Assert.Empty(result.Items);
    }

    private static async Task SubmitAsync(AcademicHarness harness, Assignment assignment, Student student, int? grade = null)
    {
        var submission = new AssignmentSubmission(assignment.Id, student.Id);
        submission.Submit("submission.pdf", Now.AddDays(-1));
        if (grade.HasValue)
        {
            submission.SetGrade(grade.Value);
        }

        harness.Context.Set<AssignmentSubmission>().Add(submission);
        await harness.Context.SaveChangesAsync();
    }
}
