using eNote.API.Controllers.Admin;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Mvc;

namespace eNote.Tests.Identity;

public sealed class AdminUsersControllerTests
{
    [Fact]
    public async Task GetPaged_ReturnsFilteredUsers()
    {
        var identity = new StubUserIdentityService(
            users: new Dictionary<int, UserIdentityDto>
            {
                [15] = new() { Id = 15, Username = "admin", FirstName = "Ada", LastName = "Admin", IsActive = true },
                [16] = new() { Id = 16, Username = "jdoe", FirstName = "Jane", LastName = "Doe", IsActive = true }
            },
            roles: new Dictionary<int, IReadOnlyList<string>>
            {
                [15] = [AppRoles.Administrator],
                [16] = [AppRoles.Student]
            });
        var profileService = new UserProfileService(null!, identity, null!, new StubCurrentActor());
        var controller = new AdminUsersController(profileService, new StubProvisioningService());

        var result = await controller.GetPaged(
            new AdminUserSearchObject { Role = AppRoles.Administrator },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var page = Assert.IsType<PagedResult<AdminUserListItemDto>>(ok.Value);
        var item = Assert.Single(page.Items);
        Assert.Equal(15, item.AppUserId);
        Assert.Equal(AppRoles.Administrator, item.Role);
    }

    [Fact]
    public async Task GetById_ThrowsNotFound_WhenUserIsMissing()
    {
        var profileService = new UserProfileService(null!, new StubUserIdentityService(), null!, new StubCurrentActor());
        var controller = new AdminUsersController(profileService, new StubProvisioningService());

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            controller.GetById(99, CancellationToken.None));

        Assert.Equal(Messages.NotFound, exception.Message);
    }

    [Fact]
    public async Task Provision_ThrowsConflict_WhenUsernameTaken()
    {
        var provisioning = new StubProvisioningService { ProvisionError = new ConflictException(Messages.UsernameTaken) };
        var controller = new AdminUsersController(null!, provisioning);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            controller.Provision(new UserProvisionRequest
            {
                Username = "taken",
                Email = "taken@example.com",
                Password = "Password1!",
                Role = AppRoles.Student
            }, CancellationToken.None));

        Assert.Equal(Messages.UsernameTaken, ex.Message);
    }

    [Fact]
    public async Task Provision_ReturnsCreated_WhenUserIsCreated()
    {
        var provisioning = new StubProvisioningService();
        var controller = new AdminUsersController(null!, provisioning);

        var result = await controller.Provision(new UserProvisionRequest
        {
            Username = "new",
            Email = "new@example.com",
            Password = "Password1!",
            Role = AppRoles.Student
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(7, created.RouteValues!["id"]);
    }

    [Fact]
    public async Task SetUserStatus_ThrowsNotFound_WhenUserMissing()
    {
        var provisioning = new StubProvisioningService { SetActiveError = new NotFoundException(Messages.NotFound) };
        var controller = new AdminUsersController(null!, provisioning);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            controller.SetUserStatus(5, new UserStatusRequest(false), CancellationToken.None));

        Assert.Equal(Messages.NotFound, exception.Message);
    }

    [Fact]
    public async Task SetUserStatus_ThrowsBusiness_WhenUpdateFails()
    {
        var provisioning = new StubProvisioningService { SetActiveError = new BusinessException("Identity update failed") };
        var controller = new AdminUsersController(null!, provisioning);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            controller.SetUserStatus(5, new UserStatusRequest(false), CancellationToken.None));

        Assert.Equal("Identity update failed", exception.Message);
    }

    [Fact]
    public async Task SetUserStatus_ThrowsConflict_WhenModifyingOwnAccount()
    {
        var provisioning = new StubProvisioningService { SetActiveError = new ConflictException(Messages.CannotModifyOwnAccount) };
        var controller = new AdminUsersController(null!, provisioning);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            controller.SetUserStatus(5, new UserStatusRequest(false), CancellationToken.None));

        Assert.Equal(Messages.CannotModifyOwnAccount, exception.Message);
    }

    [Fact]
    public async Task DeleteUser_ThrowsConflict_WhenDeletingOwnAccount()
    {
        var provisioning = new StubProvisioningService { DeleteUserError = new ConflictException(Messages.CannotModifyOwnAccount) };
        var controller = new AdminUsersController(null!, provisioning);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            controller.DeleteUser(5, CancellationToken.None));

        Assert.Equal(Messages.CannotModifyOwnAccount, exception.Message);
    }

    private sealed class StubProvisioningService : IUserProvisioningService
    {
        public Exception? ProvisionError { get; init; }
        public Exception? SetActiveError { get; init; }
        public Exception? DeleteUserError { get; init; }

        public Task<RegistrationResult> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default) =>
            ProvisionError is null ? Task.FromResult(7) : Task.FromException<int>(ProvisionError);

        public Task SetUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default) =>
            SetActiveError is null ? Task.CompletedTask : Task.FromException(SetActiveError);

        public Task DeleteUserAsync(int userId, CancellationToken cancellationToken = default) =>
            DeleteUserError is null ? Task.CompletedTask : Task.FromException(DeleteUserError);

        public Task<bool> IsStoreManagerAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<int> ProvisionStudentByInstructorAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(7);

        public Task<int> ProvisionEmployeeByManagerAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(7);
    }
}
