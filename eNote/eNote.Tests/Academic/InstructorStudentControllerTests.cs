using eNote.API.Controllers.Academic;
using eNote.Application.Common.Paging;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Students;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Mvc;

namespace eNote.Tests.Academic;

public sealed class InstructorStudentControllerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPaged_ReturnsStudents()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);
        var instructorAccess = AcademicTestData.CreateInstructorAccess(ctx, harness.Instructor);
        var studentService = new AdminStudentService(ctx, new StubUserIdentityService(), instructorAccess);
        var currentUser = new StubCurrentActor(instructor: harness.Instructor, userId: harness.Instructor.AppUserId);
        var controller = new InstructorStudentController(studentService, new StubProvisioningService(), currentUser, instructorAccess);

        var result = await controller.GetPaged(new StudentSearchObject(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paged = Assert.IsType<PagedResult<StudentDto>>(ok.Value);
        var item = Assert.Single(paged.Items);
        Assert.Equal(harness.Student.Id, item.Id);
    }

    [Fact]
    public async Task GetPaged_ReturnsOnlyStudentsEnrolledInInstructorCourses()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);

        var otherInstructor = new Instructor(200);
        ctx.Set<Instructor>().Add(otherInstructor);
        await ctx.SaveChangesAsync();

        var otherCourse = new Course("Piano 101", null, 150m, Now, Now.AddMonths(4), otherInstructor.Id)
        {
            CreatedById = otherInstructor.AppUserId
        };
        ctx.Set<Course>().Add(otherCourse);
        await ctx.SaveChangesAsync();

        var otherStudent = new Student(201, Now);
        var unenrolledStudent = new Student(301, Now);
        ctx.Set<Student>().AddRange(otherStudent, unenrolledStudent);
        await ctx.SaveChangesAsync();

        ctx.Set<Enrollment>().Add(new Enrollment(otherStudent.Id, otherCourse.Id, EnrollmentStatus.Active));
        await ctx.SaveChangesAsync();

        var instructorAccess = AcademicTestData.CreateInstructorAccess(ctx, harness.Instructor);
        var studentService = new AdminStudentService(ctx, new StubUserIdentityService(), instructorAccess);
        var currentUser = new StubCurrentActor(instructor: harness.Instructor, userId: harness.Instructor.AppUserId);
        var controller = new InstructorStudentController(studentService, new StubProvisioningService(), currentUser, instructorAccess);

        var result = await controller.GetPaged(new StudentSearchObject(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paged = Assert.IsType<PagedResult<StudentDto>>(ok.Value);
        var item = Assert.Single(paged.Items);
        Assert.Equal(harness.Student.Id, item.Id);
    }

    [Fact]
    public async Task Create_ReturnsCreatedResult_WhenSuccessful()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);
        var instructorAccess = AcademicTestData.CreateInstructorAccess(ctx, harness.Instructor);
        var studentService = new AdminStudentService(ctx, new StubUserIdentityService(), instructorAccess);
        var currentUser = new StubCurrentActor(instructor: harness.Instructor, userId: harness.Instructor.AppUserId);
        var stubProvisioning = new StubProvisioningService { CreateResult = (42, null) };
        var controller = new InstructorStudentController(studentService, stubProvisioning, currentUser, instructorAccess);

        var result = await controller.Create(new DelegatedUserCreateRequest
        {
            Username = "newstudent",
            Email = "student@example.com",
            Password = "Password1!"
        }, CancellationToken.None);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenErrorOccurs()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);
        var instructorAccess = AcademicTestData.CreateInstructorAccess(ctx, harness.Instructor);
        var studentService = new AdminStudentService(ctx, new StubUserIdentityService(), instructorAccess);
        var currentUser = new StubCurrentActor(instructor: harness.Instructor, userId: harness.Instructor.AppUserId);
        var stubProvisioning = new StubProvisioningService { CreateResult = (0, "Username already taken") };
        var controller = new InstructorStudentController(studentService, stubProvisioning, currentUser, instructorAccess);

        var result = await controller.Create(new DelegatedUserCreateRequest
        {
            Username = "existingstudent",
            Email = "student@example.com",
            Password = "Password1!"
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    private sealed class StubUserIdentityService : IUserIdentityService
    {
        public Task<UserIdentityDto?> GetUserAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserIdentityDto?>(new UserIdentityDto { Id = userId, Username = "test", FirstName = "First", LastName = "Last" });

        public Task<IReadOnlyDictionary<int, UserIdentityDto>> GetUsersBulkAsync(IEnumerable<int> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, UserIdentityDto>>(userIds.ToDictionary(id => id, id => new UserIdentityDto { Id = id, Username = $"user{id}" }));

        public Task<IReadOnlyList<string>> GetRolesAsync(int userId) =>
            Task.FromResult<IReadOnlyList<string>>(["Student"]);
    }

    private sealed class StubProvisioningService : IUserProvisioningService
    {
        public (int UserId, string? Error) CreateResult { get; set; } = (1, null);

        public Task<(RegistrationResult? Registration, string? Error)> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<(RegistrationResult?, string?)>((null, null));

        public Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult((1, (string?)null));

        public Task UpdateMembershipAsync(int userId, UpdateMembershipRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<(bool Success, string? Error)> DeactivateUserAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult((true, (string?)null));

        public Task<bool> IsStoreManagerAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<(int UserId, string? Error)> ProvisionStudentByInstructorAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateResult);

        public Task<(int UserId, string? Error)> ProvisionEmployeeByManagerAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateResult);
    }
}
