using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Profiles;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace eNote.Tests.Identity;

public sealed class UserProfileServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetCurrentUserAsync_UsesCurrentUserId()
    {
        var identity = Identity(ActiveUser(15), AppRoles.Administrator);

        var service = CreateService(identity, currentUserId: 15);

        var result = await service.GetCurrentUserAsync();

        Assert.NotNull(result);
        Assert.Equal(15, identity.LastRequestedUserId);
        Assert.Equal(AppRoles.Administrator, result.Role);
    }

    [Fact]
    public async Task GetUserAsync_ReturnsNull_WhenUserIsInactive()
    {
        var identity = Identity(new UserIdentityDto
        {
            Id = 1,
            Username = "inactive",
            IsActive = false
        }, AppRoles.Administrator);

        var service = CreateService(identity);

        var result = await service.GetUserAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserAsync_Throws_WhenUserHasMultipleRoles()
    {
        var identity = Identity(ActiveUser(1), AppRoles.Student, AppRoles.Instructor);

        var service = CreateService(identity);

        await Assert.ThrowsAsync<BusinessException>(() => service.GetUserAsync(1));
    }

    [Fact]
    public async Task GetUserAsync_BuildsAdminProfile()
    {
        var identity = Identity(new UserIdentityDto
        {
            Id = 3,
            Username = "admin",
            FirstName = "Admin",
            LastName = "User",
            DateOfBirth = new DateTime(1990, 5, 12),
            IsActive = true
        }, AppRoles.Administrator);

        var service = CreateService(identity);

        var result = await service.GetUserAsync(3);

        Assert.NotNull(result);
        Assert.Equal(AppRoles.Administrator, result.Role);
        var profile = Assert.IsType<AdminProfile>(result.Profile);
        Assert.Equal("Admin", profile.FirstName);
        Assert.Equal("User", profile.LastName);
        Assert.Equal(new DateTime(1990, 5, 12), profile.DateOfBirth);
    }

    [Fact]
    public async Task GetUserAsync_BuildsStudentProfile()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(appUserId: 4, enrollmentDate: new DateTime(2026, 1, 10));
        student.UpdateMembership(new DateTime(2026, 12, 31));
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();

        var identity = Identity(new UserIdentityDto { Id = 4, Username = "student", FirstName = "Stu", LastName = "Dent", IsActive = true }, AppRoles.Student);

        var service = new UserProfileService(context, identity, new StubUserProfileLookup(student: student), new StubCurrentActor(userId: 4));

        var result = await service.GetUserAsync(4);

        Assert.NotNull(result);
        var profile = Assert.IsType<StudentProfile>(result.Profile);
        Assert.Equal(student.Id, profile.Id);
        Assert.Equal(student.EnrollmentDate, profile.EnrollmentDate);
        Assert.Equal(student.MembershipPaidUntil, profile.MembershipPaidUntil);
        Assert.Equal("Stu", profile.FirstName);
    }

    [Fact]
    public async Task GetUserAsync_BuildsInstructorProfile()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var instructor = new Instructor(appUserId: 5);
        context.Set<Instructor>().Add(instructor);
        await context.SaveChangesAsync();

        var identity = Identity(new UserIdentityDto { Id = 5, Username = "instructor", FirstName = "Pro", LastName = "Fessor", IsActive = true }, AppRoles.Instructor);

        var service = new UserProfileService(context, identity, new StubUserProfileLookup(instructor: instructor), new StubCurrentActor(userId: 5));

        var result = await service.GetUserAsync(5);

        Assert.NotNull(result);
        var profile = Assert.IsType<InstructorProfile>(result.Profile);
        Assert.Equal(instructor.Id, profile.Id);
        Assert.Equal("Pro", profile.FirstName);
        Assert.Equal("Fessor", profile.LastName);
    }

    [Fact]
    public async Task GetUserAsync_BuildsStoreEmployeeProfile()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();
        var employee = new MusicStoreEmployee(appUserId: 7, musicStoreId: store.Id, isManager: true);
        context.Set<MusicStoreEmployee>().Add(employee);
        await context.SaveChangesAsync();

        var identity = Identity(ActiveUser(7), AppRoles.StoreEmployee);

        var service = new UserProfileService(context, identity, new UserProfileLookup(context), new StubCurrentActor(userId: 7));

        var result = await service.GetUserAsync(7, includeInactive: false);

        Assert.NotNull(result);
        var profile = Assert.IsType<MusicStoreProfile>(result.Profile);
        Assert.Equal(store.Id, profile.Id);
        Assert.Equal("Music Shop", profile.StoreName);
        Assert.True(profile.IsManager);
    }

    [Fact]
    public async Task GetUserAsync_ThrowsStoreNotFound_WhenEmployeeStoreIsMissing()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var employee = new MusicStoreEmployee(appUserId: 7, musicStoreId: 999, isManager: false);
        var identity = Identity(ActiveUser(7), AppRoles.StoreEmployee);

        var service = new UserProfileService(context, identity, new StubUserProfileLookup(employee: employee), new StubCurrentActor(userId: 7));

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.GetUserAsync(7, includeInactive: false));
        Assert.Equal(Messages.StoreNotFound, ex.Message);
    }

    [Fact]
    public async Task GetUserAsync_IncludeInactive_ReturnsEmployeeProfile_WhenEmployeeInactive()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var employee = new MusicStoreEmployee(appUserId: 7, musicStoreId: store.Id, isManager: false) { IsActive = false };
        context.Set<MusicStoreEmployee>().Add(employee);
        await context.SaveChangesAsync();

        var identity = Identity(ActiveUser(7), AppRoles.StoreEmployee);

        var service = new UserProfileService(context, identity, new UserProfileLookup(context), new StubCurrentActor(userId: 7));

        var result = await service.GetUserAsync(7, includeInactive: true);

        Assert.NotNull(result);
        var profile = Assert.IsType<MusicStoreProfile>(result.Profile);
        Assert.Equal(store.Id, profile.Id);
        Assert.False(profile.IsManager);
    }

    [Fact]
    public async Task GetUserAsync_ExcludeInactive_ThrowsEmployeeProfileNotFound_WhenEmployeeInactive()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var employee = new MusicStoreEmployee(appUserId: 7, musicStoreId: store.Id, isManager: false) { IsActive = false };
        context.Set<MusicStoreEmployee>().Add(employee);
        await context.SaveChangesAsync();

        var identity = Identity(ActiveUser(7), AppRoles.StoreEmployee);

        var service = new UserProfileService(context, identity, new UserProfileLookup(context), new StubCurrentActor(userId: 7));

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.GetUserAsync(7, includeInactive: false));
        Assert.Equal(Messages.EmployeeProfileNotFound, ex.Message);
    }

    private static UserProfileService CreateService(StubUserIdentityService identity, int currentUserId = 1) =>
        new(new ThrowingDbContext(), identity, new ThrowingUserProfileLookup(), new StubCurrentActor(userId: currentUserId));

    private static StubUserIdentityService Identity(UserIdentityDto user, params string[] roles) =>
        new(
            users: new Dictionary<int, UserIdentityDto> { [user.Id] = user },
            roles: new Dictionary<int, IReadOnlyList<string>> { [user.Id] = roles });

    private static UserIdentityDto ActiveUser(int id) => new()
    {
        Id = id,
        Username = $"user{id}",
        IsActive = true
    };

    private sealed class ThrowingUserProfileLookup : IUserProfileLookup
    {
        public Task<Student> GetStudentAsync(int userId) => throw new NotSupportedException();
        public Task<Instructor> GetInstructorAsync(int userId) => throw new NotSupportedException();
        public Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId) => throw new NotSupportedException();
    }

    private sealed class ThrowingDbContext : IAppDbContext
    {
        public DbSet<TEntity> Set<TEntity>() where TEntity : class => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
