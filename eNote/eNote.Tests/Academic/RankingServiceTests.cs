using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Assignments;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Academic;

public sealed class RankingServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetForInstructorAsync_RanksStudentsByAverageGrade()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var secondStudent = new Student(60, Now);
        harness.Context.Set<Student>().Add(secondStudent);
        await harness.Context.SaveChangesAsync();
        harness.Context.Set<Enrollment>().Add(new Enrollment(secondStudent.Id, harness.Course.Id, EnrollmentStatus.Active));
        await harness.Context.SaveChangesAsync();

        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();

        harness.Context.Set<AssignmentSubmission>().AddRange(
            CreateSubmission(assignment.Id, harness.Student.Id, grade: 90),
            CreateSubmission(assignment.Id, secondStudent.Id, grade: 70),
            CreateSubmission(assignment.Id, harness.Student.Id, grade: 80));
        await harness.Context.SaveChangesAsync();

        var service = CreateService(harness.Context, harness.Instructor, harness.Student);
        var ranking = await service.GetForInstructorAsync(harness.Course.Id);

        Assert.Equal(2, ranking.Count);
        Assert.Equal(1, ranking[0].Rank);
        Assert.Equal(harness.Student.Id, ranking[0].StudentId);
        Assert.Equal(85, ranking[0].AverageGrade);
        Assert.Equal(2, ranking[0].GradedSubmissions);
        Assert.Equal(2, ranking[1].Rank);
        Assert.Equal(70, ranking[1].AverageGrade);
    }

    [Fact]
    public async Task GetForInstructorAsync_Throws_WhenCourseNotOwned()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherInstructor = new Instructor(300);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, otherInstructor, harness.Student);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetForInstructorAsync(harness.Course.Id));
    }

    [Fact]
    public async Task GetForStudentAsync_Throws_WhenNotEnrolled()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var stranger = new Student(77, Now);
        harness.Context.Set<Student>().Add(stranger);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, stranger);

        await Assert.ThrowsAsync<AuthorizationException>(() => service.GetForStudentAsync(harness.Course.Id));
    }

    [Fact]
    public async Task GetForStudentAsync_ReturnsEmpty_WhenNoGrades()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor, harness.Student);

        var ranking = await service.GetForStudentAsync(harness.Course.Id);

        Assert.Empty(ranking);
    }

    private static AssignmentSubmission CreateSubmission(int assignmentId, int studentId, int? grade)
    {
        var submission = new AssignmentSubmission(assignmentId, studentId);
        submission.Submit($"/api/uploads/assignments/{Guid.NewGuid():N}.pdf", Now);
        if (grade.HasValue)
        {
            submission.SetGrade(grade.Value);
        }

        return submission;
    }

    private static RankingService CreateService(ENoteContext context, Instructor instructor, Student student)
    {
        var currentUser = new StubCurrentActor(student: student);
        return new(context,
            currentUser,
            currentUser,
            new StubDisplayNameService(),
            AcademicTestData.CreateInstructorAccess(context, instructor));
    }

    private sealed class StubDisplayNameService : IStudentDisplayNameService
    {
        public Task<string> GetStudentDisplayNameAsync(Student student) => Task.FromResult($"Student {student.Id}");
        public Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students) =>
            Task.FromResult<IReadOnlyDictionary<int, string>>(students.ToDictionary(s => s.Id, s => $"Student {s.Id}"));
    }
}
