using eNote.API.Controllers.Shop;
using eNote.Application.Common.Paging;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Employees;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Identity;
using eNote.Domain.Entities.Rentals;
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
        var stubProvisioning = new StubProvisioningService { CreateResult = (55, null) };
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
    public async Task Create_ReturnsBadRequest_WhenErrorOccurs()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var actor = new StubCurrentActor(userId: 10);
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, new StubProvisioningService());
        var stubProvisioning = new StubProvisioningService { CreateResult = (0, "Creation error") };
        var controller = new ShopEmployeeController(employeeService, stubProvisioning);

        var result = await controller.Create(new DelegatedUserCreateRequest
        {
            Username = "existingemployee",
            Email = "emp@example.com",
            Password = "Password1!"
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
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
        var stubProvisioning = new StubProvisioningService { SetActiveResult = (false, "Cannot deactivate user.") };
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, stubProvisioning);
        var controller = new ShopEmployeeController(employeeService, stubProvisioning);

        var result = await controller.SetStatus(20, new UserStatusRequest(false), CancellationToken.None);

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
            Task.FromResult<IReadOnlyList<string>>(["StoreEmployee"]);
    }

    private sealed class StubProvisioningService : IUserProvisioningService
    {
        public (int UserId, string? Error) CreateResult { get; set; } = (1, null);
        public (bool Success, string? Error) SetActiveResult { get; set; } = (true, null);

        public Task<(RegistrationResult? Registration, string? Error)> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<(RegistrationResult?, string?)>((null, null));

        public Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult((1, (string?)null));

        public Task UpdateMembershipAsync(int userId, UpdateMembershipRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<(bool Success, string? Error)> DeactivateUserAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult((true, (string?)null));
        public Task<(bool Success, string? Error)> SetUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default) => Task.FromResult(SetActiveResult);
        public Task<(bool Success, string? Error)> DeleteUserAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult((true, (string?)null));
        public Task<bool> IsStoreManagerAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<(int UserId, string? Error)> ProvisionStudentByInstructorAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateResult);

        public Task<(int UserId, string? Error)> ProvisionEmployeeByManagerAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateResult);
    }
}
