using eNote.API.Controllers.Admin;
using eNote.Application.Common.Localization;
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
    public async Task Provision_ThrowsConflict_WhenUsernameTaken()
    {
        var provisioning = new StubProvisioningService { ProvisionError = Messages.UsernameTaken };
        var controller = new AdminUsersController(null!, provisioning, new StubCurrentActor());

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
    public async Task Provision_ReturnsBadRequest_WhenOtherError()
    {
        var provisioning = new StubProvisioningService { ProvisionError = "some other error" };
        var controller = new AdminUsersController(null!, provisioning, new StubCurrentActor());

        var result = await controller.Provision(new UserProvisionRequest
        {
            Username = "new",
            Email = "new@example.com",
            Password = "Password1!",
            Role = AppRoles.Student
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SetUserStatus_ReturnsNotFound_WhenUserMissing()
    {
        var provisioning = new StubProvisioningService { SetActiveResult = (false, Messages.NotFound) };
        var controller = new AdminUsersController(null!, provisioning, new StubCurrentActor());

        var result = await controller.SetUserStatus(5, new UserStatusRequest(false), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task SetUserStatus_ReturnsBadRequest_WhenUpdateFails()
    {
        var provisioning = new StubProvisioningService { SetActiveResult = (false, "Identity update failed") };
        var controller = new AdminUsersController(null!, provisioning, new StubCurrentActor());

        var result = await controller.SetUserStatus(5, new UserStatusRequest(false), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private sealed class StubProvisioningService : IUserProvisioningService
    {
        public string? ProvisionError { get; init; }
        public (bool Success, string? Error) SetActiveResult { get; init; } = (true, null);

        public Task<(RegistrationResult? Registration, string? Error)> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<(RegistrationResult?, string?)>((null, null));

        public Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult((0, ProvisionError));

        public Task UpdateMembershipAsync(int userId, UpdateMembershipRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<(bool Success, string? Error)> DeactivateUserAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult((true, (string?)null));
        public Task<(bool Success, string? Error)> SetUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default) => Task.FromResult(SetActiveResult);
        public Task<(bool Success, string? Error)> DeleteUserAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult((true, (string?)null));
        public Task<bool> IsStoreManagerAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<(int UserId, string? Error)> ProvisionStudentByInstructorAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) => Task.FromResult((0, (string?)null));
        public Task<(int UserId, string? Error)> ProvisionEmployeeByManagerAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) => Task.FromResult((0, (string?)null));
    }
}
