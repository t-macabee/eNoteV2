using eNote.API.Controllers.Shop;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Employees;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Mvc;

namespace eNote.Tests.Rentals;

public sealed class ShopEmployeeControllerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPaged_ReturnsEmployees_ForCurrentStore()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Main Shop", "09-17");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();

        var emp1 = new MusicStoreEmployee(appUserId: 10, musicStoreId: store.Id, isManager: true);
        var emp2 = new MusicStoreEmployee(appUserId: 11, musicStoreId: store.Id, isManager: false);
        ctx.Set<MusicStoreEmployee>().AddRange(emp1, emp2);
        await ctx.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 10);
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, new StubProvisioningService());
        var controller = new ShopEmployeeController(employeeService, new StubProvisioningService());

        var result = await controller.GetPaged(new ShopEmployeeSearchObject(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paged = Assert.IsType<PagedResult<ShopEmployeeDto>>(ok.Value);
        Assert.Single(paged.Items);
        Assert.Equal(11, paged.Items[0].AppUserId);
        Assert.DoesNotContain(paged.Items, x => x.AppUserId == 10);
    }

    [Fact]
    public async Task Create_ReturnsCreatedResult_WhenSuccessful()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var actor = new StubCurrentActor(userId: 10);
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, new StubProvisioningService());
        var stubProvisioning = new StubProvisioningService { CreateUserId = 55 };
        var controller = new ShopEmployeeController(employeeService, stubProvisioning);

        var result = await controller.Create(new DelegatedUserCreateRequest
        {
            Username = "newemployee",
            Email = "emp@example.com",
            Password = "Password1!"
        }, CancellationToken.None);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Create_ThrowsBusiness_WhenCreationFails()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var actor = new StubCurrentActor(userId: 10);
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, new StubProvisioningService());
        var stubProvisioning = new StubProvisioningService { CreateError = new BusinessException("Creation error") };
        var controller = new ShopEmployeeController(employeeService, stubProvisioning);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => controller.Create(new DelegatedUserCreateRequest
        {
            Username = "existingemployee",
            Email = "emp@example.com",
            Password = "Password1!"
        }, CancellationToken.None));

        Assert.Equal("Creation error", exception.Message);
    }

    [Fact]
    public async Task SetStatus_ReturnsNoContent_WhenSuccessful()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Main Shop", "09-17");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();

        var manager = new MusicStoreEmployee(appUserId: 10, musicStoreId: store.Id, isManager: true);
        var employee = new MusicStoreEmployee(appUserId: 20, musicStoreId: store.Id, isManager: false);
        ctx.Set<MusicStoreEmployee>().AddRange(manager, employee);
        await ctx.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 10);
        var stubProvisioning = new StubProvisioningService();
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, stubProvisioning);
        var controller = new ShopEmployeeController(employeeService, stubProvisioning);

        var result = await controller.SetStatus(20, new UserStatusRequest(false), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SetStatus_ReturnsConflict_WhenModifyingOwnAccount()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Main Shop", "09-17");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();

        var manager = new MusicStoreEmployee(appUserId: 10, musicStoreId: store.Id, isManager: true);
        ctx.Set<MusicStoreEmployee>().Add(manager);
        await ctx.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 10);
        var stubProvisioning = new StubProvisioningService { SetActiveError = new ConflictException(Messages.CannotModifyOwnAccount) };
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, stubProvisioning);
        var controller = new ShopEmployeeController(employeeService, stubProvisioning);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            controller.SetStatus(10, new UserStatusRequest(false), CancellationToken.None));

        Assert.Equal(Messages.CannotModifyOwnAccount, exception.Message);
    }

    [Fact]
    public async Task SetStatus_ReturnsBadRequest_WhenErrorOccurs()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Main Shop", "09-17");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();

        var manager = new MusicStoreEmployee(appUserId: 10, musicStoreId: store.Id, isManager: true);
        var employee = new MusicStoreEmployee(appUserId: 20, musicStoreId: store.Id, isManager: false);
        ctx.Set<MusicStoreEmployee>().AddRange(manager, employee);
        await ctx.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 10);
        var stubProvisioning = new StubProvisioningService { SetActiveError = new BusinessException("Cannot deactivate user.") };
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, stubProvisioning);
        var controller = new ShopEmployeeController(employeeService, stubProvisioning);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            controller.SetStatus(20, new UserStatusRequest(false), CancellationToken.None));

        Assert.Equal("Cannot deactivate user.", exception.Message);
    }

    [Fact]
    public async Task SetStatus_ReturnsNotFound_WhenUserMissing()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Main Shop", "09-17");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();

        var manager = new MusicStoreEmployee(appUserId: 10, musicStoreId: store.Id, isManager: true);
        var employee = new MusicStoreEmployee(appUserId: 30, musicStoreId: store.Id, isManager: false);
        ctx.Set<MusicStoreEmployee>().AddRange(manager, employee);
        await ctx.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 10);
        var stubProvisioning = new StubProvisioningService { SetActiveError = new NotFoundException(Messages.NotFound) };
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, stubProvisioning);
        var controller = new ShopEmployeeController(employeeService, stubProvisioning);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            controller.SetStatus(30, new UserStatusRequest(false), CancellationToken.None));

        Assert.Equal(Messages.NotFound, exception.Message);
    }

    [Fact]
    public async Task SetStatus_ThrowsNotFound_WhenTargetBelongsToDifferentStore()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var store1 = new MusicStore("Main Shop", "09-17");
        var store2 = new MusicStore("Other Shop", "09-17");
        ctx.Set<MusicStore>().AddRange(store1, store2);
        await ctx.SaveChangesAsync();

        var manager = new MusicStoreEmployee(appUserId: 10, musicStoreId: store1.Id, isManager: true);
        var foreignEmployee = new MusicStoreEmployee(appUserId: 20, musicStoreId: store2.Id, isManager: false);
        ctx.Set<MusicStoreEmployee>().AddRange(manager, foreignEmployee);
        await ctx.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 10);
        var stubProvisioning = new StubProvisioningService();
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, stubProvisioning);
        var controller = new ShopEmployeeController(employeeService, stubProvisioning);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            controller.SetStatus(20, new UserStatusRequest(false), CancellationToken.None));

        Assert.Equal(Messages.NotFound, ex.Message);
    }

    private sealed class StubProvisioningService : IUserProvisioningService
    {
        public int CreateUserId { get; set; } = 1;
        public Exception? CreateError { get; set; }
        public Exception? SetActiveError { get; set; }

        public Task<RegistrationResult> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(1);

        public Task SetUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default) =>
            SetActiveError is null ? Task.CompletedTask : Task.FromException(SetActiveError);

        public Task DeleteUserAsync(int userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> IsStoreManagerAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<int> ProvisionStudentByInstructorAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(1);

        public Task<int> ProvisionEmployeeByManagerAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            CreateError is null ? Task.FromResult(CreateUserId) : Task.FromException<int>(CreateError);
    }
}
