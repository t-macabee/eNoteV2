using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Students;
using eNote.Application.Features.Identity.Users;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Identity;

public sealed class AdminStudentServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPagedAsync_FiltersByNameAndUsername()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<Student>().AddRange(
            new Student(1, Now),
            new Student(2, Now),
            new Student(3, Now));
        await context.SaveChangesAsync();
        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [1] = StubUserIdentityService.User(1, "jdoe", "Jane", "Doe"),
            [2] = StubUserIdentityService.User(2, "asmith", "Alice", "Smith"),
            [3] = StubUserIdentityService.User(3, "brown", "Bob", "Brown")
        });
        var instructorAccess = new InstructorAccessService(context, new StubUserProfileLookup(instructor: new Instructor(100)));
        var service = new AdminStudentService(context, identity, instructorAccess);

        var result = await service.GetPagedAsync(new StudentSearchObject { Name = "Smith" });

        var dto = Assert.Single(result.Items);
        Assert.Equal("Alice", dto.FirstName);
        Assert.Equal("Smith", dto.LastName);
    }

    [Fact]
    public async Task GetPagedAsync_RespectsPaging()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<Student>().AddRange(
            new Student(1, Now),
            new Student(2, Now),
            new Student(3, Now));
        await context.SaveChangesAsync();
        var identity = new StubUserIdentityService();
        var instructorAccess = new InstructorAccessService(context, new StubUserProfileLookup(instructor: new Instructor(100)));
        var service = new AdminStudentService(context, identity, instructorAccess);

        var result = await service.GetPagedAsync(new StudentSearchObject { Page = 2, PageSize = 1 });

        var dto = Assert.Single(result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, dto.AppUserId);
    }

    [Fact]
    public async Task GetByIdAsync_Throws_WhenStudentMissing()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var instructorAccess = new InstructorAccessService(context, new StubUserProfileLookup(instructor: new Instructor(100)));
        var service = new AdminStudentService(context, new StubUserIdentityService(), instructorAccess);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(42, Now);
        student.UpdateMembership(Now.AddMonths(1));
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [42] = StubUserIdentityService.User(42, "jdoe", "Jane", "Doe")
        });
        var instructorAccess = new InstructorAccessService(context, new StubUserProfileLookup(instructor: new Instructor(100)));
        var service = new AdminStudentService(context, identity, instructorAccess);

        var dto = await service.GetByIdAsync(1);

        Assert.Equal(42, dto.AppUserId);
        Assert.Equal("Jane", dto.FirstName);
        Assert.Equal("jdoe", dto.Username);
        Assert.Equal(Now, dto.EnrollmentDate);
        Assert.Equal(Now.AddMonths(1), dto.MembershipPaidUntil);
    }

    [Fact]
    public async Task GetPagedForInstructorAsync_ReturnsOnlyStudentsEnrolledInInstructorCourses()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);

        var instructor1 = new Instructor(100);
        var instructor2 = new Instructor(200);
        context.Set<Instructor>().AddRange(instructor1, instructor2);
        await context.SaveChangesAsync();

        var course1 = new Course("Guitar 101", null, 100m, Now, Now.AddMonths(3), instructor1.Id) { CreatedById = instructor1.AppUserId };
        var course1b = new Course("Guitar 102", null, 120m, Now, Now.AddMonths(3), instructor1.Id) { CreatedById = instructor1.AppUserId };
        var course2 = new Course("Violin 101", null, 150m, Now, Now.AddMonths(3), instructor2.Id) { CreatedById = instructor2.AppUserId };
        context.Set<Course>().AddRange(course1, course1b, course2);
        await context.SaveChangesAsync();

        var student1 = new Student(10, Now);
        var student2 = new Student(20, Now);
        var student3 = new Student(30, Now);
        context.Set<Student>().AddRange(student1, student2, student3);
        await context.SaveChangesAsync();

        // student1 is enrolled in both course1 and course1b (instructor 1)
        // student2 is enrolled in course2 (instructor 2)
        // student3 is not enrolled in any course
        context.Set<Enrollment>().AddRange(
            new Enrollment(student1.Id, course1.Id, EnrollmentStatus.Active),
            new Enrollment(student1.Id, course1b.Id, EnrollmentStatus.Active),
            new Enrollment(student2.Id, course2.Id, EnrollmentStatus.Active));
        await context.SaveChangesAsync();

        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [10] = StubUserIdentityService.User(10, "sone", "Student", "One"),
            [20] = StubUserIdentityService.User(20, "stwo", "Student", "Two"),
            [30] = StubUserIdentityService.User(30, "sthree", "Student", "Three")
        });

        var instructorAccess = new InstructorAccessService(context, new StubUserProfileLookup(instructor: instructor1));
        var service = new AdminStudentService(context, identity, instructorAccess);

        var instructor1Result = await service.GetPagedForInstructorAsync(instructor1.Id, new StudentSearchObject { IncludeTotalCount = true });
        var instructor1Item = Assert.Single(instructor1Result.Items);
        Assert.Equal(student1.Id, instructor1Item.Id);
        Assert.Equal(1, instructor1Result.TotalCount);

        var instructor2Result = await service.GetPagedForInstructorAsync(instructor2.Id, new StudentSearchObject { IncludeTotalCount = true });
        var instructor2Item = Assert.Single(instructor2Result.Items);
        Assert.Equal(student2.Id, instructor2Item.Id);
        Assert.Equal(1, instructor2Result.TotalCount);

        // Admin unscoped query still returns all students
        var adminResult = await service.GetPagedAsync(new StudentSearchObject { IncludeTotalCount = true });
        Assert.Equal(3, adminResult.Items.Count);
        Assert.Equal(3, adminResult.TotalCount);
    }
}
