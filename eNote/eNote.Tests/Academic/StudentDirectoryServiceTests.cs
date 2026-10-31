using eNote.Application.Features.Academic.StudentDirectory;
using eNote.Application.Features.Identity.Users;
using eNote.Domain.Entities.Assignments;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Academic;

public sealed class StudentDirectoryServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPagedAsync_ListsStudentWithoutEnrollmentInInstructorsCourses()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        await AcademicTestData.SeedAsync(context, Now);
        var student = await AddStudentAsync(context, 70);
        var service = CreateService(context, CreateIdentity(User(70, "marko", "Marko", "Markovic")));

        var result = await service.GetPagedAsync(new StudentDirectorySearchObject());

        var entry = Assert.Single(result.Items, x => x.Id == student.Id);
        Assert.Equal(student.AppUserId, entry.AppUserId);
        Assert.Equal("marko", entry.Username);
    }

    [Fact]
    public async Task GetPagedAsync_ExcludesInactiveUsers()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var active = await AddStudentAsync(context, 70);
        var inactive = await AddStudentAsync(context, 71);
        var service = CreateService(context, CreateIdentity(
            User(70, "active"),
            User(71, "inactive", isActive: false)));

        var result = await service.GetPagedAsync(new StudentDirectorySearchObject());

        Assert.Contains(result.Items, x => x.Id == active.Id);
        Assert.DoesNotContain(result.Items, x => x.Id == inactive.Id);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByName()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var marko = await AddStudentAsync(context, 70);
        await AddStudentAsync(context, 71);
        var service = CreateService(context, CreateIdentity(
            User(70, "marko", "Marko", "Markovic"),
            User(71, "jana", "Jana", "Jovanovic")));

        var result = await service.GetPagedAsync(new StudentDirectorySearchObject { Name = "marko" });

        var entry = Assert.Single(result.Items);
        Assert.Equal(marko.Id, entry.Id);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsCoursesOfAllInstructors_WithStatusAverageAndCount()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(context, Now);
        var other = await AcademicTestData.AddSecondInstructorContentAsync(harness, Now);
        var student = await AddStudentAsync(context, 70);
        await EnrollAsync(context, student, harness.Course, EnrollmentStatus.Active);
        await EnrollAsync(context, student, other.Course, EnrollmentStatus.Completed);

        var firstCourseAssignment = await AddAssignmentAsync(context, harness.Lecture, "First homework");
        var secondCourseAssignment = await AddAssignmentAsync(context, harness.Lecture, "Second homework");
        await GradeAsync(context, firstCourseAssignment.Id, student.Id, 4);
        await GradeAsync(context, secondCourseAssignment.Id, student.Id, 5);

        var otherFirstAssignment = await AddAssignmentAsync(context, other.Lecture, "First piano homework");
        var otherSecondAssignment = await AddAssignmentAsync(context, other.Lecture, "Second piano homework");
        var otherThirdAssignment = await AddAssignmentAsync(context, other.Lecture, "Third piano homework");
        await GradeAsync(context, otherFirstAssignment.Id, student.Id, 3);
        await GradeAsync(context, otherSecondAssignment.Id, student.Id, 4);
        await GradeAsync(context, otherThirdAssignment.Id, student.Id, 4);

        var service = CreateService(context, CreateIdentity(
            User(70, "student", "Student", "One"),
            User(100, "instructor1", "Ivan", "Prvi"),
            User(200, "instructor2", "Petar", "Drugi")));

        var profile = await service.GetProfileAsync(student.Id);

        Assert.Equal(2, profile.Courses.Count);

        var firstCourse = profile.Courses[0];
        Assert.Equal("Guitar 101", firstCourse.CourseName);
        Assert.Equal("Ivan Prvi", firstCourse.InstructorName);
        Assert.Equal(EnrollmentStatus.Active, firstCourse.EnrollmentStatus);
        Assert.Equal(4.5, firstCourse.AverageGrade);
        Assert.Equal(2, firstCourse.GradedSubmissions);

        var secondCourse = profile.Courses[1];
        Assert.Equal("Piano 101", secondCourse.CourseName);
        Assert.Equal("Petar Drugi", secondCourse.InstructorName);
        Assert.Equal(EnrollmentStatus.Completed, secondCourse.EnrollmentStatus);
        Assert.Equal(3.67, secondCourse.AverageGrade);
        Assert.Equal(3, secondCourse.GradedSubmissions);
    }

    [Fact]
    public async Task GetProfileAsync_ExcludesPendingAndRejectedEnrollments()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(context, Now);
        var other = await AcademicTestData.AddSecondInstructorContentAsync(harness, Now);
        var student = await AddStudentAsync(context, 70);
        await EnrollAsync(context, student, harness.Course, EnrollmentStatus.Active);
        await EnrollAsync(context, student, other.Course, EnrollmentStatus.Pending);

        var rejectedInstructor = new Instructor(300);
        context.Set<Instructor>().Add(rejectedInstructor);
        await context.SaveChangesAsync();
        var rejectedCourse = new Course("Violin 101", null, 100m, Now, Now.AddMonths(6), rejectedInstructor.Id)
        {
            CreatedById = rejectedInstructor.AppUserId
        };
        context.Set<Course>().Add(rejectedCourse);
        await context.SaveChangesAsync();
        await EnrollAsync(context, student, rejectedCourse, EnrollmentStatus.Rejected);

        var service = CreateService(context, CreateIdentity(
            User(70, "student"),
            User(100, "instructor1"),
            User(200, "instructor2"),
            User(300, "instructor3")));

        var profile = await service.GetProfileAsync(student.Id);

        var course = Assert.Single(profile.Courses);
        Assert.Equal(harness.Course.Id, course.CourseId);
    }

    [Fact]
    public async Task GetProfileAsync_IgnoresGradesOnIndividualLectureOfAnotherStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(context, Now);
        var student = await AddStudentAsync(context, 70);
        await EnrollAsync(context, student, harness.Course, EnrollmentStatus.Active);
        var otherStudent = await AddStudentAsync(context, 71);

        var individualLecture = await AcademicTestData.AddIndividualLectureAsync(harness, otherStudent, Now.AddDays(1));
        var individualAssignment = await AddAssignmentAsync(context, individualLecture, "Individual plan");
        await GradeAsync(context, individualAssignment.Id, student.Id, 5);

        var classAssignment = await AddAssignmentAsync(context, harness.Lecture, "Class homework");
        await GradeAsync(context, classAssignment.Id, student.Id, 4);

        var service = CreateService(context, CreateIdentity(
            User(70, "student"),
            User(71, "otherStudent"),
            User(100, "instructor1")));

        var profile = await service.GetProfileAsync(student.Id);

        var course = Assert.Single(profile.Courses);
        Assert.Equal(4, course.AverageGrade);
        Assert.Equal(1, course.GradedSubmissions);
    }

    [Fact]
    public async Task GetProfileAsync_CourseWithoutGrades_HasNullAverageAndZeroCount()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(context, Now);
        var student = await AddStudentAsync(context, 70);
        await EnrollAsync(context, student, harness.Course, EnrollmentStatus.Active);
        var service = CreateService(context, CreateIdentity(
            User(70, "student"),
            User(100, "instructor1")));

        var profile = await service.GetProfileAsync(student.Id);

        var course = Assert.Single(profile.Courses);
        Assert.Null(course.AverageGrade);
        Assert.Equal(0, course.GradedSubmissions);
    }

    [Fact]
    public async Task GetProfileAsync_Throws_WhenUserIsInactive()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = await AddStudentAsync(context, 70);
        var service = CreateService(context, CreateIdentity(User(70, "student", isActive: false)));

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetProfileAsync(student.Id));
    }

    [Fact]
    public async Task GetProfileAsync_Throws_WhenStudentDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context, CreateIdentity());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetProfileAsync(9999));
    }

    private static StudentDirectoryService CreateService(ENoteContext context, StubUserIdentityService identity) =>
        new(context, identity);

    private static StubUserIdentityService CreateIdentity(params UserIdentityDto[] users) =>
        new(users.ToDictionary(u => u.Id));

    private static UserIdentityDto User(
        int id,
        string username,
        string? firstName = null,
        string? lastName = null,
        bool isActive = true) => new()
    {
        Id = id,
        Username = username,
        FirstName = firstName,
        LastName = lastName,
        IsActive = isActive
    };

    private static async Task<Student> AddStudentAsync(ENoteContext context, int appUserId)
    {
        var student = new Student(appUserId, Now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        return student;
    }

    private static async Task EnrollAsync(ENoteContext context, Student student, Course course, EnrollmentStatus status)
    {
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, status));
        await context.SaveChangesAsync();
    }

    private static async Task<Assignment> AddAssignmentAsync(ENoteContext context, Lecture lecture, string title)
    {
        var assignment = new Assignment(title, "Do it", Now.AddDays(7), lecture.Id);
        context.Set<Assignment>().Add(assignment);
        await context.SaveChangesAsync();
        return assignment;
    }

    private static async Task GradeAsync(ENoteContext context, int assignmentId, int studentId, int grade)
    {
        var submission = new AssignmentSubmission(assignmentId, studentId);
        submission.Submit($"/api/uploads/assignments/{Guid.NewGuid():N}.pdf", Now);
        submission.SetGrade(grade);
        context.Set<AssignmentSubmission>().Add(submission);
        await context.SaveChangesAsync();
    }
}
