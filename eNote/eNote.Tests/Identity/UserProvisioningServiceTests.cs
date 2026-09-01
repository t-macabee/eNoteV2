using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Identity;

public sealed class UserProvisioningServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RegisterStudentAsync_CreatesStudentProfile()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService();
        var service = CreateService(context, account);
        var request = new RegisterRequest { Username = "newstudent", Email = "new@example.com", Password = "Password1!" };

        var (registration, error) = await service.RegisterStudentAsync(request);

        Assert.Null(error);
        Assert.NotNull(registration);
        Assert.Equal(7, registration.UserId);
        Assert.Equal(["Student"], registration.Roles);
        Assert.True(await context.Set<Student>().AnyAsync(s => s.AppUserId == 7));
    }

    [Fact]
    public async Task RegisterStudentAsync_ReturnsError_WhenCreationFails()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService { CreateUserId = null };
        var service = CreateService(context, account);

        var (registration, error) = await service.RegisterStudentAsync(new RegisterRequest
        {
            Username = "newstudent",
            Email = "new@example.com",
            Password = "Password1!"
        });

        Assert.Null(registration);
        Assert.Equal("creation failed", error);
    }

    [Fact]
    public async Task ProvisionUserAsync_CreatesInstructorProfile()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService();
        var service = CreateService(context, account);

        var (userId, error) = await service.ProvisionUserAsync(new UserProvisionRequest
        {
            Username = "newinstructor",
            Email = "inst@example.com",
            Password = "Password1!",
            Role = AppRoles.Instructor
        });

        Assert.Null(error);
        Assert.Equal(7, userId);
        Assert.True(await context.Set<Instructor>().AnyAsync(i => i.AppUserId == 7));
    }

    [Fact]
    public async Task ProvisionUserAsync_CreatesStoreEmployee_WithDefaultStore()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();
        var account = new RecordingUserAccountService();
        var service = CreateService(context, account);

        var (userId, error) = await service.ProvisionUserAsync(new UserProvisionRequest
        {
            Username = "employee",
            Email = "emp@example.com",
            Password = "Password1!",
            Role = AppRoles.StoreEmployee
        });

        Assert.Null(error);
        Assert.Equal(7, userId);
        var employee = await context.Set<MusicStoreEmployee>().SingleAsync(e => e.AppUserId == 7);
        Assert.Equal(store.Id, employee.MusicStoreId);
        Assert.True(employee.IsActive);
    }

    [Fact]
    public async Task ProvisionUserAsync_UpdatesExistingUser_WhenUsernameExists()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService { ExistingUserId = 42 };
        var service = CreateService(context, account);

        var (userId, error) = await service.ProvisionUserAsync(new UserProvisionRequest
        {
            Username = "existing",
            Email = "existing@example.com",
            Password = "Password1!",
            Role = AppRoles.Student
        });

        Assert.Null(error);
        Assert.Equal(42, userId);
        Assert.True(account.UpdatedExisting);
    }

    [Fact]
    public async Task UpdateMembershipAsync_UpdatesStudent()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(5, Now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        var service = CreateService(context, new RecordingUserAccountService());

        await service.UpdateMembershipAsync(5, new UpdateMembershipRequest { PaidUntil = Now.AddMonths(1) });

        var updated = await context.Set<Student>().SingleAsync(s => s.AppUserId == 5);
        Assert.True(updated.HasActiveMembership(Now));
    }

    [Fact]
    public async Task UpdateMembershipAsync_Throws_WhenStudentMissing()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = CreateService(context, new RecordingUserAccountService());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateMembershipAsync(999, new UpdateMembershipRequest { PaidUntil = Now.AddMonths(1) }));
    }

    [Fact]
    public async Task DeactivateUserAsync_DelegatesToSetActiveWithFalse()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService();
        var service = CreateService(context, account);

        var (success, error) = await service.DeactivateUserAsync(42);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal((42, false), account.SetActiveCall);
    }

    [Fact]
    public async Task DeactivateUserAsync_ReturnsError_WhenAccountServiceFails()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService { SetActiveResult = (false, Messages.NotFound) };
        var service = CreateService(context, account);

        var (success, error) = await service.DeactivateUserAsync(999);

        Assert.False(success);
        Assert.Equal(Messages.NotFound, error);
    }

    private static UserProvisioningService CreateService(ENoteContext context, IUserAccountService account) =>
        new(context, account, new FixedClock(Now));

    private sealed class RecordingUserAccountService : IUserAccountService
    {
        public int? CreateUserId { get; set; } = 7;
        public int? ExistingUserId { get; set; }
        public bool UpdatedExisting { get; private set; }
        public (int UserId, bool IsActive)? SetActiveCall { get; private set; }
        public (bool Success, string? Error) SetActiveResult { get; set; } = (true, null);

        public Task<int?> FindUserIdByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExistingUserId);

        public Task<(int? UserId, string? Error)> CreateUserAsync(string username, string email, string password, string? firstName, string? lastName, CancellationToken cancellationToken = default) =>
            Task.FromResult((CreateUserId, CreateUserId is null ? "creation failed" : null));

        public Task<(bool Success, string? Error)> AssignSingleRoleAsync(int userId, string role, CancellationToken cancellationToken = default) =>
            Task.FromResult<(bool Success, string? Error)>((true, null));

        public Task<(bool Success, string? Error)> UpdateExistingUserAsync(int userId, string email, string? firstName, string? lastName, DateTime? dateOfBirth = null, CancellationToken cancellationToken = default)
        {
            UpdatedExisting = true;
            return Task.FromResult((true, (string?)null));
        }

        public Task<(bool Success, string? Error)> UpdatePictureAsync(int userId, Stream picture, string fileName, string contentType, CancellationToken cancellationToken = default) =>
            Task.FromResult((true, (string?)null));

        public Task<(Stream? Data, string? ContentType)> GetPictureAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<(Stream?, string?)>((null, null));

        public Task<(bool Success, string? Error)> DeletePictureAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult((true, (string?)null));

        public Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default) =>
            Task.FromResult((true, (string?)null));

        public Task<(bool Success, string? Error)> SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
        {
            SetActiveCall = (userId, isActive);
            return Task.FromResult(SetActiveResult);
        }
    }
}
