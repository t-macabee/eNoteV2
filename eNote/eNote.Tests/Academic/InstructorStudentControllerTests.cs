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
        var controller = new InstructorStudentController(studentService, CreateMembershipService(ctx, studentService, currentUser), new StubProvisioningService(), currentUser, instructorAccess);

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
        var controller = new InstructorStudentController(studentService, CreateMembershipService(ctx, studentService, currentUser), new StubProvisioningService(), currentUser, instructorAccess);

        var result = await controller.GetPaged(new StudentSearchObject(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paged = Assert.IsType<PagedResult<StudentDto>>(ok.Value);
        var item = Assert.Single(paged.Items);
        Assert.Equal(harness.Student.Id, item.Id);
    }

    [Fact]
    public async Task GetById_ReturnsStudent_WhenEnrolledInInstructorCourse()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);
        var instructorAccess = AcademicTestData.CreateInstructorAccess(ctx, harness.Instructor);
        var studentService = new AdminStudentService(ctx, new StubUserIdentityService(), instructorAccess);
        var currentUser = new StubCurrentActor(instructor: harness.Instructor, userId: harness.Instructor.AppUserId);
        var controller = new InstructorStudentController(studentService, CreateMembershipService(ctx, studentService, currentUser), new StubProvisioningService(), currentUser, instructorAccess);

        var result = await controller.GetById(harness.Student.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<StudentDto>(ok.Value);
        Assert.Equal(harness.Student.Id, dto.Id);
    }

    [Fact]
    public async Task GetById_ThrowsNotFoundException_WhenStudentNotEnrolledInInstructorCourses()
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
        ctx.Set<Student>().Add(otherStudent);
        await ctx.SaveChangesAsync();

        ctx.Set<Enrollment>().Add(new Enrollment(otherStudent.Id, otherCourse.Id, EnrollmentStatus.Active));
        await ctx.SaveChangesAsync();

        var unenrolledStudent = new Student(301, Now);
        ctx.Set<Student>().Add(unenrolledStudent);
        await ctx.SaveChangesAsync();

        var instructorAccess = AcademicTestData.CreateInstructorAccess(ctx, harness.Instructor);
        var studentService = new AdminStudentService(ctx, new StubUserIdentityService(), instructorAccess);
        var currentUser = new StubCurrentActor(instructor: harness.Instructor, userId: harness.Instructor.AppUserId);
        var controller = new InstructorStudentController(studentService, CreateMembershipService(ctx, studentService, currentUser), new StubProvisioningService(), currentUser, instructorAccess);

        await Assert.ThrowsAsync<NotFoundException>(() => controller.GetById(otherStudent.Id, CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => controller.GetById(unenrolledStudent.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetEnrollments_ReturnsEnrollments_WhenEnrolledInInstructorCourse()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);
        var instructorAccess = AcademicTestData.CreateInstructorAccess(ctx, harness.Instructor);
        var studentService = new AdminStudentService(ctx, new StubUserIdentityService(), instructorAccess);
        var currentUser = new StubCurrentActor(instructor: harness.Instructor, userId: harness.Instructor.AppUserId);
        var controller = new InstructorStudentController(studentService, CreateMembershipService(ctx, studentService, currentUser), new StubProvisioningService(), currentUser, instructorAccess);

        var result = await controller.GetEnrollments(harness.Student.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var enrollments = Assert.IsType<List<StudentEnrollmentDto>>(ok.Value);
        var item = Assert.Single(enrollments);
        Assert.Equal(harness.Course.Id, item.CourseId);
    }

    [Fact]
    public async Task GetEnrollments_ThrowsNotFoundException_WhenStudentNotEnrolledInInstructorCourses()
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
        ctx.Set<Student>().Add(otherStudent);
        await ctx.SaveChangesAsync();

        ctx.Set<Enrollment>().Add(new Enrollment(otherStudent.Id, otherCourse.Id, EnrollmentStatus.Active));
        await ctx.SaveChangesAsync();

        var instructorAccess = AcademicTestData.CreateInstructorAccess(ctx, harness.Instructor);
        var studentService = new AdminStudentService(ctx, new StubUserIdentityService(), instructorAccess);
        var currentUser = new StubCurrentActor(instructor: harness.Instructor, userId: harness.Instructor.AppUserId);
        var controller = new InstructorStudentController(studentService, CreateMembershipService(ctx, studentService, currentUser), new StubProvisioningService(), currentUser, instructorAccess);

        await Assert.ThrowsAsync<NotFoundException>(() => controller.GetEnrollments(otherStudent.Id, CancellationToken.None));
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
        var controller = new InstructorStudentController(studentService, CreateMembershipService(ctx, studentService, currentUser), stubProvisioning, currentUser, instructorAccess);

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
        var controller = new InstructorStudentController(studentService, CreateMembershipService(ctx, studentService, currentUser), stubProvisioning, currentUser, instructorAccess);

        var result = await controller.Create(new DelegatedUserCreateRequest
        {
            Username = "existingstudent",
            Email = "student@example.com",
            Password = "Password1!"
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task RecordMembershipPayment_ReturnsCreated_AndExtendsMembership()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);
        var instructorAccess = AcademicTestData.CreateInstructorAccess(ctx, harness.Instructor);
        var studentService = new AdminStudentService(ctx, new StubUserIdentityService(), instructorAccess);
        var currentUser = new StubCurrentActor(instructor: harness.Instructor, userId: harness.Instructor.AppUserId);
        var controller = new InstructorStudentController(studentService, CreateMembershipService(ctx, studentService, currentUser), new StubProvisioningService(), currentUser, instructorAccess);

        var result = await controller.RecordMembershipPayment(harness.Student.Id, new RecordMembershipPaymentRequest
        {
            Amount = 40m,
            ReceiptNumber = "CL-100",
            Months = 1
        }, CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        var payment = Assert.IsType<MembershipPaymentDto>(created.Value);
        Assert.Equal("CL-100", payment.ReceiptNumber);
        Assert.Equal(harness.Student.Id, payment.StudentId);
    }

    [Fact]
    public async Task GetMembershipPayments_ReturnsPagedPayments()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);
        ctx.Set<MembershipPayment>().Add(new MembershipPayment(harness.Student.Id, 40m, Now.Date, Now.Date, Now.Date.AddMonths(1), "CL-101", null, harness.Instructor.AppUserId));
        await ctx.SaveChangesAsync();

        var instructorAccess = AcademicTestData.CreateInstructorAccess(ctx, harness.Instructor);
        var studentService = new AdminStudentService(ctx, new StubUserIdentityService(), instructorAccess);
        var currentUser = new StubCurrentActor(instructor: harness.Instructor, userId: harness.Instructor.AppUserId);
        var controller = new InstructorStudentController(studentService, CreateMembershipService(ctx, studentService, currentUser), new StubProvisioningService(), currentUser, instructorAccess);

        var result = await controller.GetMembershipPayments(harness.Student.Id, new MembershipPaymentSearchObject(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paged = Assert.IsType<PagedResult<MembershipPaymentDto>>(ok.Value);
        var payment = Assert.Single(paged.Items);
        Assert.Equal("CL-101", payment.ReceiptNumber);
    }

    [Fact]
    public async Task SendMembershipWarning_ReturnsNoContent_WhenMembershipExpiresSoon()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);
        harness.Student.UpdateMembership(Now.Date.AddDays(3));
        await ctx.SaveChangesAsync();

        var instructorAccess = AcademicTestData.CreateInstructorAccess(ctx, harness.Instructor);
        var studentService = new AdminStudentService(ctx, new StubUserIdentityService(), instructorAccess);
        var currentUser = new StubCurrentActor(instructor: harness.Instructor, userId: harness.Instructor.AppUserId);
        var controller = new InstructorStudentController(studentService, CreateMembershipService(ctx, studentService, currentUser), new StubProvisioningService(), currentUser, instructorAccess);

        var result = await controller.SendMembershipWarning(harness.Student.Id, new SendMembershipWarningRequest(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    private static MembershipService CreateMembershipService(
        ENoteContext context,
        AdminStudentService students,
        StubCurrentActor currentUser) => new(
            context,
            students,
            new FixedClock(Now),
            currentUser,
            new RecordingMembershipWarningDispatcher());

    private sealed class StubProvisioningService : IUserProvisioningService
    {
        public (int UserId, string? Error) CreateResult { get; set; } = (1, null);

        public Task<(RegistrationResult? Registration, string? Error)> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<(RegistrationResult?, string?)>((null, null));

        public Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult((1, (string?)null));

        public Task<(bool Success, string? Error)> SetUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default) => Task.FromResult((true, (string?)null));
        public Task<(bool Success, string? Error)> DeleteUserAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult((true, (string?)null));
        public Task<bool> IsStoreManagerAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<(int UserId, string? Error)> ProvisionStudentByInstructorAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateResult);

        public Task<(int UserId, string? Error)> ProvisionEmployeeByManagerAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateResult);
    }
}
