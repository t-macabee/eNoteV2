using eNote.API.Controllers.Shop;
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

        var emp = new MusicStoreEmployee(appUserId: 10, musicStoreId: store.Id, isManager: true);
        ctx.Set<MusicStoreEmployee>().Add(emp);
        await ctx.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 10);
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor);
        var controller = new ShopEmployeeController(employeeService, new StubProvisioningService());

        var result = await controller.GetPaged(new ShopEmployeeSearchObject(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Create_ReturnsCreatedResult_WhenSuccessful()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var actor = new StubCurrentActor(userId: 10);
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor);
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
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor);
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
