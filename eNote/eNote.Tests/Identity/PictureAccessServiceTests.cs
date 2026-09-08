using eNote.API.Controllers.Users;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eNote.Tests.Identity;

/// <summary>
/// Mirrors <c>FileAccessServiceTests</c>: one case per visibility-table row
/// (allow and deny), plus the two C0 landmine guards. Every deny means the
/// controller returns a bare 404 (see the controller tests below) — never a
/// 403 or BusinessException — so denies are marked with `// → 404`.
/// </summary>
public sealed class PictureAccessServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    // ── Administrator → everyone ──────────────────────────────────────

    [Fact]
    public async Task Admin_CanView_StudentPicture()
    {
        var seed = await SeedAsync();
        var service = CreateService(seed.Context, viewerUserId: 99, new FixedStoreContext(1),
            new Dictionary<int, IReadOnlyList<string>> { [99] = [AppRoles.Administrator] });

        Assert.True(await service.CanViewPictureAsync(seed.StudentA.AppUserId));
    }

    [Fact]
    public async Task Admin_CanView_InstructorPicture()
    {
        var seed = await SeedAsync();
        var service = CreateService(seed.Context, viewerUserId: 99, new FixedStoreContext(1),
            new Dictionary<int, IReadOnlyList<string>> { [99] = [AppRoles.Administrator] });

        Assert.True(await service.CanViewPictureAsync(seed.Instructor1.AppUserId));
    }

    [Fact]
    public async Task Admin_CanView_EmployeePicture()
    {
        var seed = await SeedAsync();
        var service = CreateService(seed.Context, viewerUserId: 99, new FixedStoreContext(1),
            new Dictionary<int, IReadOnlyList<string>> { [99] = [AppRoles.Administrator] });

        Assert.True(await service.CanViewPictureAsync(seed.Employee1.AppUserId));
    }

    // ── Instructor ────────────────────────────────────────────────────

    [Fact]
    public async Task Instructor_CanView_OtherInstructorPicture()
    {
        var seed = await SeedAsync();
        var service = CreateService(seed.Context, seed.Instructor1.AppUserId, new FixedStoreContext(1),
            new Dictionary<int, IReadOnlyList<string>> { [seed.Instructor1.AppUserId] = [AppRoles.Instructor] });

        Assert.True(await service.CanViewPictureAsync(seed.Instructor2.AppUserId));
    }

    [Fact]
    public async Task Instructor_CanView_EnrolledStudentPicture()
    {
        var seed = await SeedAsync();
        var service = CreateService(seed.Context, seed.Instructor1.AppUserId, new FixedStoreContext(1),
            new Dictionary<int, IReadOnlyList<string>> { [seed.Instructor1.AppUserId] = [AppRoles.Instructor] });

        // StudentA is enrolled in Instructor1's course.
        Assert.True(await service.CanViewPictureAsync(seed.StudentA.AppUserId));
    }

    [Fact]
    public async Task Instructor_CanView_UnenrolledStudentPicture()
    {
        var seed = await SeedAsync();
        var service = CreateService(seed.Context, seed.Instructor1.AppUserId, new FixedStoreContext(1),
            new Dictionary<int, IReadOnlyList<string>> { [seed.Instructor1.AppUserId] = [AppRoles.Instructor] });

        // StudentB has no enrolment anywhere — still visible (no enrolment join).
        Assert.True(await service.CanViewPictureAsync(seed.StudentB.AppUserId));
    }

    [Fact]
    public async Task Instructor_CannotView_EmployeePicture()
    {
        var seed = await SeedAsync();
        var service = CreateService(seed.Context, seed.Instructor1.AppUserId, new FixedStoreContext(1),
            new Dictionary<int, IReadOnlyList<string>> { [seed.Instructor1.AppUserId] = [AppRoles.Instructor] });

        Assert.False(await service.CanViewPictureAsync(seed.Employee1.AppUserId)); // → 404
    }

    // ── StoreEmployee ─────────────────────────────────────────────────

    [Fact]
    public async Task Employee_CanView_SameStoreEmployeePicture()
    {
        var seed = await SeedAsync();
        var service = CreateService(seed.Context, seed.Employee1.AppUserId, new FixedStoreContext(seed.Store1.Id),
            new Dictionary<int, IReadOnlyList<string>> { [seed.Employee1.AppUserId] = [AppRoles.StoreEmployee] });

        Assert.True(await service.CanViewPictureAsync(seed.Employee2.AppUserId));
    }

    [Fact]
    public async Task Employee_CannotView_OtherStoreEmployeePicture()
    {
        var seed = await SeedAsync();
        var service = CreateService(seed.Context, seed.Employee1.AppUserId, new FixedStoreContext(seed.Store1.Id),
            new Dictionary<int, IReadOnlyList<string>> { [seed.Employee1.AppUserId] = [AppRoles.StoreEmployee] });

        Assert.False(await service.CanViewPictureAsync(seed.Employee3.AppUserId)); // → 404
    }

    [Fact]
    public async Task Employee_CanView_StudentWithRentalAtOwnStore()
    {
        var seed = await SeedAsync();
        var service = CreateService(seed.Context, seed.Employee1.AppUserId, new FixedStoreContext(seed.Store1.Id),
            new Dictionary<int, IReadOnlyList<string>> { [seed.Employee1.AppUserId] = [AppRoles.StoreEmployee] });

        Assert.True(await service.CanViewPictureAsync(seed.StudentA.AppUserId));
    }

    [Fact]
    public async Task Employee_CannotView_StudentWithNoRental()
    {
        var seed = await SeedAsync();
        var service = CreateService(seed.Context, seed.Employee1.AppUserId, new FixedStoreContext(seed.Store1.Id),
            new Dictionary<int, IReadOnlyList<string>> { [seed.Employee1.AppUserId] = [AppRoles.StoreEmployee] });

        Assert.False(await service.CanViewPictureAsync(seed.StudentB.AppUserId)); // → 404
    }

    // ── Student ───────────────────────────────────────────────────────

    [Fact]
    public async Task Student_CanView_InstructorPicture()
    {
        var seed = await SeedAsync();
        var service = CreateService(seed.Context, seed.StudentA.AppUserId, new FixedStoreContext(1),
            new Dictionary<int, IReadOnlyList<string>> { [seed.StudentA.AppUserId] = [AppRoles.Student] });

        Assert.True(await service.CanViewPictureAsync(seed.Instructor1.AppUserId));
    }

    [Fact]
    public async Task Student_CannotView_OtherStudentPicture()
    {
        var seed = await SeedAsync();
        var service = CreateService(seed.Context, seed.StudentA.AppUserId, new FixedStoreContext(1),
            new Dictionary<int, IReadOnlyList<string>> { [seed.StudentA.AppUserId] = [AppRoles.Student] });

        Assert.False(await service.CanViewPictureAsync(seed.StudentB.AppUserId)); // → 404
    }

    [Fact]
    public async Task Student_CannotView_EmployeePicture()
    {
        var seed = await SeedAsync();
        var service = CreateService(seed.Context, seed.StudentA.AppUserId, new FixedStoreContext(1),
            new Dictionary<int, IReadOnlyList<string>> { [seed.StudentA.AppUserId] = [AppRoles.Student] });

        Assert.False(await service.CanViewPictureAsync(seed.Employee1.AppUserId)); // → 404
    }

    // ── Self ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Anyone_CanView_OwnPicture()
    {
        var seed = await SeedAsync();
        var service = CreateService(seed.Context, seed.StudentA.AppUserId, new FixedStoreContext(1),
            new Dictionary<int, IReadOnlyList<string>> { [seed.StudentA.AppUserId] = [AppRoles.Student] });

        Assert.True(await service.CanViewPictureAsync(seed.StudentA.AppUserId));
    }

    // ── C0 landmines ──────────────────────────────────────────────────

    [Fact]
    public async Task Employee_UnresolvedStore_DeniesOtherStoreEmployeePicture()
    {
        // Fail-closed guard (C0.1): with no store resolved the tenant filter
        // matches everything, so an implementation relying on the filter
        // instead of an explicit MusicStoreId comparison would allow this.
        var seed = await SeedAsync();
        seed.Context.ExplicitStoreId = null;
        var service = CreateService(seed.Context, seed.Employee3.AppUserId, new ThrowingStoreContext(),
            new Dictionary<int, IReadOnlyList<string>> { [seed.Employee3.AppUserId] = [AppRoles.StoreEmployee] });

        Assert.False(await service.CanViewPictureAsync(seed.Employee1.AppUserId)); // → 404
    }

    [Fact]
    public async Task ProfileId_AsId_DoesNotResolveAnotherUser()
    {
        // Student.Id is not AppUser.Id: passing the profile id must not
        // return the wrong person's image.
        var seed = await SeedAsync();
        Assert.NotEqual(seed.StudentA.Id, seed.StudentA.AppUserId);

        var service = CreateService(seed.Context, seed.Instructor1.AppUserId, new FixedStoreContext(1),
            new Dictionary<int, IReadOnlyList<string>> { [seed.Instructor1.AppUserId] = [AppRoles.Instructor] });

        Assert.False(await service.CanViewPictureAsync(seed.StudentA.Id)); // → 404
        Assert.True(await service.CanViewPictureAsync(seed.StudentA.AppUserId));
    }

    // ── Controller: every failure is a bare 404 ───────────────────────

    [Fact]
    public async Task GetUserPicture_ReturnsNotFound_WhenAccessDenied()
    {
        var controller = CreateController(canView: false, picture: (new MemoryStream([1]), "image/jpeg"));

        var result = await controller.GetUserPicture(50, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetUserPicture_ReturnsNotFound_WhenNoPicture()
    {
        var controller = CreateController(canView: true, picture: (null, null));

        var result = await controller.GetUserPicture(50, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetUserPicture_ReturnsFile_WithNoStoreCacheHeaders_WhenAllowed()
    {
        var controller = CreateController(canView: true, picture: (new MemoryStream([1, 2, 3]), "image/jpeg"));

        var result = await controller.GetUserPicture(50, CancellationToken.None);

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/jpeg", fileResult.ContentType);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl.ToString());
        Assert.Equal("Authorization", controller.Response.Headers.Vary.ToString());
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static PictureAccessService CreateService(
        ENoteContext context,
        int viewerUserId,
        IStoreContext stores,
        Dictionary<int, IReadOnlyList<string>> roles) =>
        new(context,
            new StubCurrentActor(userId: viewerUserId),
            stores,
            new StubUserIdentityService(roles: roles));

    private static UsersController CreateController(bool canView, (Stream? Data, string? ContentType) picture)
    {
        var controller = new UsersController(null!, null!, new AllowFixed(canView), new PictureFixed(picture))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return controller;
    }

    private sealed class FixedStoreContext(int storeId) : IStoreContext
    {
        public Task<int> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(storeId);
    }

    private sealed class ThrowingStoreContext : IStoreContext
    {
        public Task<int> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default) =>
            throw new StoreNotResolvedException("active employee store not found");
    }

    private sealed class AllowFixed(bool allowed) : IPictureAccessService
    {
        public Task<bool> CanViewPictureAsync(int targetAppUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(allowed);
    }

    private sealed class PictureFixed((Stream? Data, string? ContentType) picture) : IUserAccountService
    {
        public Task<int?> FindUserIdByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(int? UserId, string? Error)> CreateUserAsync(string username, string email, string password, string? firstName, string? lastName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(bool Success, string? Error)> AssignSingleRoleAsync(int userId, string role, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(bool Success, string? Error)> UpdateExistingUserAsync(int userId, string email, string? firstName, string? lastName, DateTime? dateOfBirth = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(bool Success, string? Error)> UpdatePictureAsync(int userId, Stream picture, string fileName, string contentType, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(Stream? Data, string? ContentType)> GetPictureAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(picture);
        public Task<(bool Success, string? Error)> DeletePictureAsync(int userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(bool Success, string? Error)> SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(bool Success, string? Error)> DeleteUserAsync(int userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private static async Task<PictureSeed> SeedAsync()
    {
        var context = TestDbContextFactory.CreateContext(Now);

        var instructor1 = new Instructor(100);
        var instructor2 = new Instructor(101);
        context.Set<Instructor>().AddRange(instructor1, instructor2);
        await context.SaveChangesAsync();

        var course = new Course("Guitar 101", null, 100m, Now, Now.AddMonths(6), instructor1.Id)
        {
            CreatedById = instructor1.AppUserId
        };
        course.SetPublishedStatus(true);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();

        var studentA = new Student(50, Now);
        var studentB = new Student(51, Now);
        context.Set<Student>().AddRange(studentA, studentB);
        await context.SaveChangesAsync();

        context.Set<Enrollment>().Add(new Enrollment(studentA.Id, course.Id, EnrollmentStatus.Active));
        await context.SaveChangesAsync();

        var store1 = new MusicStore("Store One", "09-17");
        var store2 = new MusicStore("Store Two", "09-17");
        context.Set<MusicStore>().AddRange(store1, store2);
        await context.SaveChangesAsync();

        var employee1 = new MusicStoreEmployee(60, store1.Id, false);
        var employee2 = new MusicStoreEmployee(61, store1.Id, false);
        var employee3 = new MusicStoreEmployee(62, store2.Id, false);
        context.Set<MusicStoreEmployee>().AddRange(employee1, employee2, employee3);
        await context.SaveChangesAsync();

        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        context.Set<InstrumentType>().Add(type);
        await context.SaveChangesAsync();

        var instrument = new Instrument("Stradivarius", "Yamaha", null, null, type.Id, store1.Id);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();

        // Any status counts: a plain requested (Pending) rental links the
        // student to the store.
        context.Set<InstrumentRental>().Add(new InstrumentRental(instrument.Id, studentA.Id, store1.Id, Now, null));
        await context.SaveChangesAsync();

        return new PictureSeed(context, instructor1, instructor2, studentA, studentB, store1, store2, employee1, employee2, employee3);
    }

    private sealed record PictureSeed(
        ENoteContext Context,
        Instructor Instructor1,
        Instructor Instructor2,
        Student StudentA,
        Student StudentB,
        MusicStore Store1,
        MusicStore Store2,
        MusicStoreEmployee Employee1,
        MusicStoreEmployee Employee2,
        MusicStoreEmployee Employee3);
}
