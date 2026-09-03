using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Academic;

public sealed class CourseServiceTests
{
    // NOTE: DeleteAsync's happy path deactivates the course's Lectures via ExecuteUpdateAsync,
    // which the EF Core InMemory test provider cannot translate. Same reason
    // NotificationService.MarkAllReadAsync has no unit test here. Only the 404 branch (which
    // runs before ExecuteUpdate) is unit-testable under the current harness.

    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_CreatesCourse_ForCurrentInstructor()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);
        var request = new CourseRequest { Name = "Piano", Price = 150m, StartDate = Now, EndDate = Now.AddMonths(3), IsPublished = false };

        var dto = await service.CreateAsync(request);

        Assert.Equal("Piano", dto.Name);
        var row = await harness.Context.Set<Course>().SingleAsync(c => c.Name == "Piano");
        Assert.Equal(harness.Instructor.Id, row.InstructorId);
    }

    [Fact]
    public async Task GetByIdForInstructorAsync_Throws_WhenCourseBelongsToAnotherInstructor()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherInstructor = new Instructor(200);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness.Context, otherInstructor);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForInstructorAsync(harness.Course.Id));
    }

    [Fact]
    public async Task GetByIdForStudentAsync_ReturnsCourse_WhenEnrolled()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var actor = new StubCurrentActor(student: harness.Student);
        var service = CreateService(harness.Context, harness.Instructor, actor);

        var dto = await service.GetByIdForStudentAsync(harness.Course.Id);

        Assert.Equal(harness.Course.Id, dto.Id);
    }

    [Fact]
    public async Task GetByIdForStudentAsync_Throws_WhenCourseUnpublishedAndNotEnrolled()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var course = new Course("Private", null, 100m, Now, null, harness.Instructor.Id);
        course.SetPublishedStatus(false);
        harness.Context.Set<Course>().Add(course);
        await harness.Context.SaveChangesAsync();
        var actor = new StubCurrentActor(student: new Student(77, Now));
        var service = CreateService(harness.Context, harness.Instructor, actor);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForStudentAsync(course.Id));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOwnedCourse()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);
        var request = new CourseRequest { Name = "Renamed", Price = 200m, StartDate = Now, EndDate = Now.AddMonths(2), IsPublished = true };

        var dto = await service.UpdateAsync(harness.Course.Id, request);

        Assert.Equal("Renamed", dto.Name);
        Assert.True(dto.IsPublished);
    }

    [Fact]
    public async Task GetPagedForAdminAsync_ReturnsCoursesAcrossInstructors()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherInstructor = new Instructor(200);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        var otherCourse = new Course("Violin", null, 90m, Now, Now.AddMonths(4), otherInstructor.Id);
        otherCourse.SetPublishedStatus(true);
        harness.Context.Set<Course>().Add(otherCourse);
        await harness.Context.SaveChangesAsync();
        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [100] = StubUserIdentityService.User(100, "jdoe", "Jane", "Doe"),
            [200] = StubUserIdentityService.User(200, "asmith", "Alice", "Smith")
        });
        var service = CreateService(harness.Context, harness.Instructor, identity: identity);

        var result = await service.GetPagedForAdminAsync(new CourseSearchObject());

        Assert.Equal(2, result.Items.Count);
        var dto = result.Items.Single(c => c.Name == "Violin");
        Assert.Equal("Alice Smith", dto.InstructorName);
        Assert.True(result.Items.Single(c => c.Name == "Guitar 101").InstructorName == "Jane Doe");
    }

    [Fact]
    public async Task GetPagedForAdminAsync_FiltersByName()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);

        var result = await service.GetPagedForAdminAsync(new CourseSearchObject { Name = "Violin" });

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetByIdForAdminAsync_ReturnsCourse_OfAnyInstructor()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherInstructor = new Instructor(200);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        var otherCourse = new Course("Violin", null, 90m, Now, Now.AddMonths(4), otherInstructor.Id);
        harness.Context.Set<Course>().Add(otherCourse);
        await harness.Context.SaveChangesAsync();
        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [200] = StubUserIdentityService.User(200, "asmith", "Alice", "Smith")
        });
        var service = CreateService(harness.Context, harness.Instructor, identity: identity);

        var dto = await service.GetByIdForAdminAsync(otherCourse.Id);

        Assert.Equal(otherCourse.Id, dto.Id);
        Assert.Equal("Alice Smith", dto.InstructorName);
    }

    [Fact]
    public async Task GetByIdForAdminAsync_Throws_WhenCourseMissing()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness.Context, harness.Instructor);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForAdminAsync(999));
    }

    [Fact]
    public async Task GetPagedForAdminAsync_WithRealIdentityService_DoesNotThrowOnMultiInstructorPage()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var userManager = IdentityTestHarness.Create(context).UserManager;

        var instructor = new Instructor(100);
        var otherInstructor = new Instructor(200);
        context.Set<Instructor>().Add(instructor);
        context.Set<Instructor>().Add(otherInstructor);
        var guitar = new Course("Guitar 101", null, 100m, Now, Now.AddMonths(6), instructor.Id);
        guitar.SetPublishedStatus(true);
        var violin = new Course("Violin", null, 90m, Now, Now.AddMonths(4), otherInstructor.Id);
        violin.SetPublishedStatus(true);
        context.Set<Course>().Add(guitar);
        context.Set<Course>().Add(violin);
        await context.SaveChangesAsync();

        await CreateActiveUserAsync(userManager, 100, "jdoe", "Jane", "Doe");
        await CreateActiveUserAsync(userManager, 200, "asmith", "Alice", "Smith");

        var service = CreateService(context, instructor, identity: new UserIdentityService(userManager));

        var result = await service.GetPagedForAdminAsync(new CourseSearchObject());

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Jane Doe", result.Items.Single(c => c.Name == "Guitar 101").InstructorName);
        Assert.Equal("Alice Smith", result.Items.Single(c => c.Name == "Violin").InstructorName);
    }

    [Fact]
    public async Task GetPagedForAdminAsync_ResolvesInstructorNamesWithSingleBulkLookup()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherInstructor = new Instructor(200);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        var otherCourse = new Course("Violin", null, 90m, Now, Now.AddMonths(4), otherInstructor.Id);
        otherCourse.SetPublishedStatus(true);
        harness.Context.Set<Course>().Add(otherCourse);
        await harness.Context.SaveChangesAsync();
        var identity = new RecordingUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [100] = StubUserIdentityService.User(100, "jdoe", "Jane", "Doe"),
            [200] = StubUserIdentityService.User(200, "asmith", "Alice", "Smith")
        });
        var service = CreateService(harness.Context, harness.Instructor, identity: identity);

        var result = await service.GetPagedForAdminAsync(new CourseSearchObject());

        Assert.Equal(1, identity.BulkCallCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Alice Smith", result.Items.Single(c => c.Name == "Violin").InstructorName);
    }

    private static async Task CreateActiveUserAsync(UserManager<AppUser> userManager, int id, string username, string firstName, string lastName)
    {
        await userManager.CreateAsync(new AppUser
        {
            Id = id,
            UserName = username,
            Email = $"{username}@example.com",
            FirstName = firstName,
            LastName = lastName,
            IsActive = true
        }, "Password1!");
    }

    private static CourseService CreateService(ENoteContext context, Instructor instructor, StubCurrentActor? actor = null, IUserIdentityService? identity = null)
    {
        var currentUser = actor ?? new StubCurrentActor(instructor: instructor);
        return new(context,
            TestMapper.Create(),
            currentUser,
            currentUser,
            AcademicTestData.CreateInstructorAccess(context, instructor),
            NullLogger<CourseService>.Instance,
            identity ?? new StubUserIdentityService());
    }

    private sealed class RecordingUserIdentityService(Dictionary<int, UserIdentityDto> users) : IUserIdentityService
    {
        public int BulkCallCount { get; private set; }

        public Task<UserIdentityDto?> GetUserAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(users.GetValueOrDefault(userId));

        public Task<IReadOnlyDictionary<int, UserIdentityDto>> GetUsersBulkAsync(IEnumerable<int> userIds, CancellationToken cancellationToken = default)
        {
            BulkCallCount++;
            return Task.FromResult<IReadOnlyDictionary<int, UserIdentityDto>>(
                users.Where(u => userIds.Contains(u.Key)).ToDictionary(u => u.Key, u => u.Value));
        }

        public Task<IReadOnlyList<string>> GetRolesAsync(int userId) =>
            Task.FromResult<IReadOnlyList<string>>([]);
    }
}
