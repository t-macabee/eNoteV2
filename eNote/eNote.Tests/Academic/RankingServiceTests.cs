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

    [Fact]
    public async Task GetForStudentAsync_ReturnsRanking_AfterCourseUnpublished_WhenEnrolledAndPaid()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        harness.Context.Set<AssignmentSubmission>().Add(CreateSubmission(assignment.Id, harness.Student.Id, grade: 90));
        harness.Context.Set<Course>().Single(c => c.Id == harness.Course.Id).SetPublishedStatus(false);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, harness.Instructor, harness.Student);

        var ranking = await service.GetForStudentAsync(harness.Course.Id);

        var entry = Assert.Single(ranking);
        Assert.Equal(harness.Student.Id, entry.StudentId);
        Assert.Equal(90, entry.AverageGrade);
    }

    [Fact]
    public async Task GetFullForInstructorAsync_OrdersByAverageThenStudentId()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var students = await SeedGradedStudentsAsync(harness, 19, harnessStudentGrade: 50);
        var assignment = harness.Context.Set<Assignment>().Single();
        var tiedStudent = new Student(1019, Now);
        harness.Context.Set<Student>().Add(tiedStudent);
        await harness.Context.SaveChangesAsync();
        harness.Context.Set<Enrollment>().Add(new Enrollment(tiedStudent.Id, harness.Course.Id, EnrollmentStatus.Active));
        harness.Context.Set<AssignmentSubmission>().Add(CreateSubmission(assignment.Id, tiedStudent.Id, grade: 99));
        await harness.Context.SaveChangesAsync();

        var service = CreateService(harness.Context, harness.Instructor, harness.Student);
        var ranking = await service.GetFullForInstructorAsync(harness.Course.Id);

        Assert.Equal(21, ranking.Count);
        Assert.Equal(Enumerable.Range(1, 21), ranking.Select(e => e.Rank));
        Assert.True(ranking.Zip(ranking.Skip(1)).All(pair => pair.First.AverageGrade >= pair.Second.AverageGrade));
        Assert.Equal(students[0].Id, ranking[0].StudentId);
        Assert.Equal(tiedStudent.Id, ranking[1].StudentId);
        Assert.True(ranking[0].StudentId < ranking[1].StudentId);
    }

    [Fact]
    public async Task GetForInstructorAsync_ReturnsTop15()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        await SeedGradedStudentsAsync(harness, 19, harnessStudentGrade: 50);
        var service = CreateService(harness.Context, harness.Instructor, harness.Student);

        var ranking = await service.GetForInstructorAsync(harness.Course.Id);

        Assert.Equal(15, ranking.Count);
        Assert.Equal(Enumerable.Range(1, 15), ranking.Select(e => e.Rank));
        Assert.DoesNotContain(ranking, e => e.StudentId == harness.Student.Id);
    }

    [Fact]
    public async Task GetForStudentAsync_ReturnsTop15PlusOwnEntry_WhenOutside()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        await SeedGradedStudentsAsync(harness, 19, harnessStudentGrade: 50);
        var service = CreateService(harness.Context, harness.Instructor, harness.Student);

        var ranking = await service.GetForStudentAsync(harness.Course.Id);

        Assert.Equal(16, ranking.Count);
        Assert.Equal(Enumerable.Range(1, 15), ranking.Take(15).Select(e => e.Rank));
        Assert.Equal(harness.Student.Id, ranking[15].StudentId);
        Assert.Equal(20, ranking[15].Rank);
        Assert.Equal(50, ranking[15].AverageGrade);
    }

    [Fact]
    public async Task GetForStudentAsync_ReturnsTop15Only_WhenInside()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        await SeedGradedStudentsAsync(harness, 19, harnessStudentGrade: 100);
        var service = CreateService(harness.Context, harness.Instructor, harness.Student);

        var ranking = await service.GetForStudentAsync(harness.Course.Id);

        Assert.Equal(15, ranking.Count);
        Assert.Equal(harness.Student.Id, ranking[0].StudentId);
        Assert.Single(ranking, e => e.StudentId == harness.Student.Id);
        Assert.Equal(Enumerable.Range(1, 15), ranking.Select(e => e.Rank));
    }

    private static async Task<List<Student>> SeedGradedStudentsAsync(AcademicHarness harness, int count, int harnessStudentGrade)
    {
        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();

        var students = new List<Student>();
        for (var i = 0; i < count; i++)
        {
            students.Add(new Student(1000 + i, Now));
        }

        harness.Context.Set<Student>().AddRange(students);
        await harness.Context.SaveChangesAsync();

        for (var i = 0; i < students.Count; i++)
        {
            harness.Context.Set<Enrollment>().Add(new Enrollment(students[i].Id, harness.Course.Id, EnrollmentStatus.Active));
            harness.Context.Set<AssignmentSubmission>().Add(CreateSubmission(assignment.Id, students[i].Id, grade: 99 - i));
        }

        harness.Context.Set<AssignmentSubmission>().Add(CreateSubmission(assignment.Id, harness.Student.Id, harnessStudentGrade));
        await harness.Context.SaveChangesAsync();

        return students;
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
            AcademicTestData.CreateInstructorAccess(context, instructor),
            new FixedClock(Now));
    }
}
