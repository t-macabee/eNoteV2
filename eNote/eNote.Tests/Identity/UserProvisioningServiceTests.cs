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
    public async Task RegisterStudentAsync_TrimsEmail()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService();
        var service = CreateService(context, account);

        await service.RegisterStudentAsync(new RegisterRequest
        {
            Username = "newstudent",
            Email = "  new@example.com  ",
            Password = "Password1!"
        });

        Assert.Equal("new@example.com", account.LastEmail);
    }

    [Fact]
    public async Task ProvisionUserAsync_TrimsEmail()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService();
        var service = CreateService(context, account);

        await service.ProvisionUserAsync(new UserProvisionRequest
        {
            Username = "newinstructor",
            Email = "  inst@example.com  ",
            Password = "Password1!",
            Role = AppRoles.Instructor
        });

        Assert.Equal("inst@example.com", account.LastEmail);
    }

    [Fact]
    public async Task RegisterStudentAsync_CreatesStudentProfile()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService();
        var service = CreateService(context, account);
        var request = new RegisterRequest { Username = "newstudent", Email = "new@example.com", Password = "Password1!" };

        var registration = await service.RegisterStudentAsync(request);

        Assert.Equal(7, registration.UserId);
        Assert.Equal(["Student"], registration.Roles);
        Assert.True(await context.Set<Student>().AnyAsync(s => s.AppUserId == 7));
    }

    [Fact]
    public async Task RegisterStudentAsync_ThrowsBusinessException_WhenCreationFails()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService { CreateUserId = null };
        var service = CreateService(context, account);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.RegisterStudentAsync(new RegisterRequest
        {
            Username = "newstudent",
            Email = "new@example.com",
            Password = "Password1!"
        }));

        Assert.Equal("creation failed", exception.Message);
    }

    [Fact]
    public async Task ProvisionUserAsync_CreatesInstructorProfile()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService();
        var service = CreateService(context, account);

        var userId = await service.ProvisionUserAsync(new UserProvisionRequest
        {
            Username = "newinstructor",
            Email = "inst@example.com",
            Password = "Password1!",
            Role = AppRoles.Instructor
        });

        Assert.Equal(7, userId);
        Assert.True(await context.Set<Instructor>().AnyAsync(i => i.AppUserId == 7));
    }

    [Fact]
    public async Task ProvisionUserAsync_ThrowsBusinessException_WhenStoreEmployeeHasNoStore()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService();
        var service = CreateService(context, account);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.ProvisionUserAsync(new UserProvisionRequest
        {
            Username = "emp",
            Email = "emp@example.com",
            Password = "Password1!",
            Role = AppRoles.StoreEmployee
        }));

        Assert.Equal(Messages.MusicStoreRequiredForEmployee, exception.Message);
        Assert.Null(account.AssignRoleCall);
    }

    [Fact]
    public async Task ProvisionUserAsync_ThrowsConflict_WhenUsernameExists()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService { ExistingUserId = 42 };
        var service = CreateService(context, account);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.ProvisionUserAsync(new UserProvisionRequest
        {
            Username = "existing",
            Email = "existing@example.com",
            Password = "Password1!",
            Role = AppRoles.Student
        }));

        Assert.Equal(Messages.UsernameTaken, exception.Message);
        Assert.False(account.UpdatedExisting);
        Assert.Null(account.SetActiveCall);
        Assert.Null(account.AssignRoleCall);
        Assert.False(await context.Set<Student>().AnyAsync(s => s.AppUserId == 42));
    }

    [Fact]
    public async Task ProvisionUserAsync_RejectsExistingUsername_WithoutUpdating()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService { ExistingUserId = 42 };
        var service = CreateService(context, account);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.ProvisionUserAsync(new UserProvisionRequest
        {
            Username = "existing",
            Email = "existing@example.com",
            Password = "Password1!",
            Role = AppRoles.Student
        }));

        Assert.Equal(Messages.UsernameTaken, exception.Message);
        Assert.False(account.UpdatedExisting);
    }

    [Fact]
    public async Task ProvisionUserAsync_RejectsExistingUsername_WithoutReactivating()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService { ExistingUserId = 42, IsActive = true };
        var service = CreateService(context, account);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.ProvisionUserAsync(new UserProvisionRequest
        {
            Username = "existing",
            Email = "existing@example.com",
            Password = "Password1!",
            Role = AppRoles.Student
        }));

        Assert.Equal(Messages.UsernameTaken, exception.Message);
        Assert.Null(account.SetActiveCall);
    }

    [Fact]
    public async Task ProvisionUserAsync_ThrowsBusinessException_WhenRoleAssignmentFails()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService
        {
            CreateUserId = 7,
            AssignRoleError = "role assignment failed"
        };
        var service = CreateService(context, account);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.ProvisionUserAsync(new UserProvisionRequest
        {
            Username = "newuser",
            Email = "new@example.com",
            Password = "Password1!",
            Role = AppRoles.Instructor
        }));

        Assert.Equal("role assignment failed", exception.Message);
        Assert.Equal((7, AppRoles.Instructor), account.AssignRoleCall);
        Assert.False(await context.Set<Instructor>().AnyAsync(i => i.AppUserId == 7));
    }

    [Fact]
    public async Task ProvisionUserAsync_FirstEmployeeIsManager_SecondEmployeeIsNotManager()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var account = new RecordingUserAccountService { CreateUserId = 10 };
        var service = CreateService(context, account);

        var firstId = await service.ProvisionUserAsync(new UserProvisionRequest
        {
            Username = "emp1",
            Email = "emp1@example.com",
            Password = "Password1!",
            Role = AppRoles.StoreEmployee,
            MusicStoreId = store.Id
        });

        var firstEmp = await context.Set<MusicStoreEmployee>().SingleAsync(e => e.AppUserId == firstId);
        Assert.True(firstEmp.IsManager);

        account.CreateUserId = 11;
        var secondId = await service.ProvisionUserAsync(new UserProvisionRequest
        {
            Username = "emp2",
            Email = "emp2@example.com",
            Password = "Password1!",
            Role = AppRoles.StoreEmployee,
            MusicStoreId = store.Id
        });

        var secondEmp = await context.Set<MusicStoreEmployee>().SingleAsync(e => e.AppUserId == secondId);
        Assert.False(secondEmp.IsManager);
    }

    [Fact]
    public async Task ProvisionStudentByInstructorAsync_CreatesStudentProfile()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService { CreateUserId = 20 };
        var service = CreateService(context, account);

        var userId = await service.ProvisionStudentByInstructorAsync(new DelegatedUserCreateRequest
        {
            Username = "delegatedstudent",
            Email = "delstudent@example.com",
            Password = "Password1!",
            FirstName = "Tarik",
            LastName = "Student"
        });

        Assert.Equal(20, userId);
        var student = await context.Set<Student>().SingleAsync(s => s.AppUserId == 20);
        Assert.NotNull(student);
    }

    [Fact]
    public async Task ProvisionEmployeeByManagerAsync_Succeeds_WhenCallerIsManager()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var managerEmployee = new MusicStoreEmployee(appUserId: 50, musicStoreId: store.Id, isManager: true);
        context.Set<MusicStoreEmployee>().Add(managerEmployee);
        await context.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 50);
        var account = new RecordingUserAccountService { CreateUserId = 51 };
        var service = CreateService(context, account, actor);

        var userId = await service.ProvisionEmployeeByManagerAsync(new DelegatedUserCreateRequest
        {
            Username = "newhire",
            Email = "newhire@example.com",
            Password = "Password1!",
            FirstName = "Amir",
            LastName = "Worker"
        });

        Assert.Equal(51, userId);
        var employee = await context.Set<MusicStoreEmployee>().SingleAsync(e => e.AppUserId == 51);
        Assert.Equal(store.Id, employee.MusicStoreId);
        Assert.False(employee.IsManager);
    }

    [Fact]
    public async Task ProvisionEmployeeByManagerAsync_ThrowsAuthorizationException_WhenCallerNotManager()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var regularEmployee = new MusicStoreEmployee(appUserId: 60, musicStoreId: store.Id, isManager: false);
        context.Set<MusicStoreEmployee>().Add(regularEmployee);
        await context.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 60);
        var account = new RecordingUserAccountService { CreateUserId = 61 };
        var service = CreateService(context, account, actor);

        var ex = await Assert.ThrowsAsync<AuthorizationException>(() =>
            service.ProvisionEmployeeByManagerAsync(new DelegatedUserCreateRequest
            {
                Username = "newhire2",
                Email = "newhire2@example.com",
                Password = "Password1!"
            }));

        Assert.Equal(Messages.ManagerRoleRequired, ex.Message);
    }

    [Fact]
    public async Task IsStoreManagerAsync_ReturnsCorrectStatus()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var manager = new MusicStoreEmployee(appUserId: 70, musicStoreId: store.Id, isManager: true);
        var regular = new MusicStoreEmployee(appUserId: 71, musicStoreId: store.Id, isManager: false);
        context.Set<MusicStoreEmployee>().AddRange(manager, regular);
        await context.SaveChangesAsync();

        var service = CreateService(context, new RecordingUserAccountService());

        Assert.True(await service.IsStoreManagerAsync(70));
        Assert.False(await service.IsStoreManagerAsync(71));
        Assert.False(await service.IsStoreManagerAsync(999));
    }

    [Fact]
    public async Task SetUserActiveAsync_PassesThroughToAccountService()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService();
        var service = CreateService(context, account);

        await service.SetUserActiveAsync(99, true);
        Assert.Equal((99, true), account.SetActiveCall);

        await service.SetUserActiveAsync(99, false);
        Assert.Equal((99, false), account.SetActiveCall);
        Assert.Null(account.SetActiveError);
    }

    [Fact]
    public async Task SetUserActiveAsync_RejectsSelfDeactivation()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var account = new RecordingUserAccountService();
        var currentUser = new StubCurrentActor(userId: 42);
        var service = CreateService(context, account, currentUser);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.SetUserActiveAsync(42, false));

        Assert.Equal(Messages.CannotModifyOwnAccount, exception.Message);
        Assert.Null(account.SetActiveCall);
    }

    [Fact]
    public async Task DeleteUserAsync_DeletesPictureAfterSuccessfulAccountDelete()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(appUserId: 15, enrollmentDate: Now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();

        var account = new RecordingUserAccountService { DeleteUserPicturePath = "/api/uploads/profile-pictures/pic.png" };
        var fileStorage = new RecordingFileStorageService();
        var service = CreateService(context, account, fileStorage: fileStorage);

        await service.DeleteUserAsync(15);

        Assert.Contains("/api/uploads/profile-pictures/pic.png", fileStorage.DeletedPaths);
    }

    [Fact]
    public async Task DeleteUserAsync_KeepsPictureWhenAccountDeleteFails()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var student = new Student(appUserId: 15, enrollmentDate: Now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();

        var account = new RecordingUserAccountService { DeleteUserError = new NotFoundException(Messages.NotFound), DeleteUserPicturePath = "/api/uploads/profile-pictures/pic.png" };
        var fileStorage = new RecordingFileStorageService();
        var service = CreateService(context, account, fileStorage: fileStorage);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteUserAsync(15));

        Assert.Equal(Messages.NotFound, exception.Message);
        Assert.Empty(fileStorage.DeletedPaths);
    }

    [Fact]
    public async Task DeleteUserAsync_RejectsSelfDelete()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var currentUser = new StubCurrentActor(userId: 42);
        var service = CreateService(context, new RecordingUserAccountService(), currentUser);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.DeleteUserAsync(42));

        Assert.Equal(Messages.CannotModifyOwnAccount, exception.Message);
    }

    private static UserProvisioningService CreateService(
        ENoteContext context,
        IUserAccountService account,
        ICurrentUserContext? currentUser = null,
        IFileStorageService? fileStorage = null) =>
        new(context, account, new FixedClock(Now), fileStorage ?? new StubFileStorageService(), currentUser ?? new StubCurrentActor());

    private sealed class RecordingUserAccountService : IUserAccountService
    {
        public int? CreateUserId { get; set; } = 7;
        public int? ExistingUserId { get; set; }
        public bool IsActive { get; set; }
        public bool UpdatedExisting { get; private set; }
        public (int UserId, bool IsActive)? SetActiveCall { get; private set; }
        public string? SetActiveError { get; set; }
        public string? AssignRoleError { get; set; }
        public (int UserId, string Role)? AssignRoleCall { get; private set; }
        public string? LastEmail { get; private set; }

        public Task<int?> FindUserIdByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExistingUserId);

        public Task<int> CreateUserAsync(string username, string email, string password, string? firstName, string? lastName, CancellationToken cancellationToken = default)
        {
            LastEmail = email;

            return CreateUserId is null
                ? throw new BusinessException("creation failed")
                : Task.FromResult(CreateUserId.Value);
        }

        public Task AssignSingleRoleAsync(int userId, string role, CancellationToken cancellationToken = default)
        {
            AssignRoleCall = (userId, role);

            return AssignRoleError is null
                ? Task.CompletedTask
                : throw new BusinessException(AssignRoleError);
        }

        public Task UpdateExistingUserAsync(int userId, string email, string? firstName, string? lastName, DateTime? dateOfBirth = null, CancellationToken cancellationToken = default)
        {
            UpdatedExisting = true;
            return Task.CompletedTask;
        }

        public Task UpdatePictureAsync(int userId, Stream picture, string fileName, string contentType, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<(Stream? Data, string? ContentType)> GetPictureAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<(Stream?, string?)>((null, null));

        public Task DeletePictureAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
        {
            SetActiveCall = (userId, isActive);

            return SetActiveError is null
                ? Task.CompletedTask
                : throw new BusinessException(SetActiveError);
        }

        public bool VerifyPasswordResult { get; set; } = true;

        public Task<bool> VerifyPasswordAsync(int userId, string currentPassword, CancellationToken cancellationToken = default) =>
            Task.FromResult(VerifyPasswordResult);

        public Task<string?> CloseAccountAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Exception? DeleteUserError { get; set; }

        public string? DeleteUserPicturePath { get; set; }

        public Task<string?> DeleteUserAsync(int userId, CancellationToken cancellationToken = default) =>
            DeleteUserError is null
                ? Task.FromResult(DeleteUserPicturePath)
                : Task.FromException<string?>(DeleteUserError);
    }
}
