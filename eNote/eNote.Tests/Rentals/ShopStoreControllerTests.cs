using eNote.API.Controllers.Shop;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Employees;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.ReferenceData.Addresses;
using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using eNote.Domain.Entities.Identity;
using eNote.Domain.Entities.Rentals;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eNote.Tests.Rentals;

public sealed class ShopStoreControllerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetOwnStore_ReturnsOk_ForAnyEmployee()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Store Alpha", "08:00-16:00");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();

        var employee = new MusicStoreEmployee(appUserId: 20, musicStoreId: store.Id, isManager: false);
        ctx.Set<MusicStoreEmployee>().Add(employee);
        await ctx.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 20);
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, null!);
        var storeService = new MusicStoreService(ctx, new StubFileStorageService());
        var storeContext = new StubStoreContext(store.Id);
        var addressService = new AddressService(ctx);
        var controller = new ShopStoreController(storeService, employeeService, addressService, storeContext);

        var result = await controller.GetOwnStore(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MusicStoreDto>(ok.Value);
        Assert.Equal(store.Id, dto.Id);
        Assert.Equal("Store Alpha", dto.StoreName);
    }

    [Fact]
    public async Task UpdateOwnStore_ReturnsOk_WhenCallerIsManager()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Store Alpha", "08:00-16:00");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();

        var manager = new MusicStoreEmployee(appUserId: 10, musicStoreId: store.Id, isManager: true);
        ctx.Set<MusicStoreEmployee>().Add(manager);
        await ctx.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 10);
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, null!);
        var storeService = new MusicStoreService(ctx, new StubFileStorageService());
        var storeContext = new StubStoreContext(store.Id);
        var addressService = new AddressService(ctx);
        var controller = new ShopStoreController(storeService, employeeService, addressService, storeContext);

        var request = new MusicStoreRequest
        {
            StoreName = "Store Alpha Updated",
            BusinessHours = "09:00-17:00",
            PhoneNumber = "+38761234567"
        };

        var result = await controller.UpdateOwnStore(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MusicStoreDto>(ok.Value);
        Assert.Equal(store.Id, dto.Id);
        Assert.Equal("Store Alpha Updated", dto.StoreName);
    }

    [Fact]
    public async Task UpdateOwnStore_ThrowsAuthorizationException_WhenCallerIsNotManager()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Store Alpha", "08:00-16:00");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();

        var regularEmployee = new MusicStoreEmployee(appUserId: 20, musicStoreId: store.Id, isManager: false);
        ctx.Set<MusicStoreEmployee>().Add(regularEmployee);
        await ctx.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 20);
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, null!);
        var storeService = new MusicStoreService(ctx, new StubFileStorageService());
        var storeContext = new StubStoreContext(store.Id);
        var addressService = new AddressService(ctx);
        var controller = new ShopStoreController(storeService, employeeService, addressService, storeContext);

        var request = new MusicStoreRequest
        {
            StoreName = "Unauthorized Change",
            BusinessHours = "09:00-17:00"
        };

        var ex = await Assert.ThrowsAsync<AuthorizationException>(() =>
            controller.UpdateOwnStore(request, CancellationToken.None));
        Assert.Equal(Messages.ManagerRoleRequired, ex.Message);
    }

    [Fact]
    public async Task UploadOwnStoreImage_ReturnsOk_WhenCallerIsManager()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Store Alpha", "08:00-16:00");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();

        var manager = new MusicStoreEmployee(appUserId: 10, musicStoreId: store.Id, isManager: true);
        ctx.Set<MusicStoreEmployee>().Add(manager);
        await ctx.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 10);
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, null!);
        var fileStorage = new StubFileStorageService();
        var storeService = new MusicStoreService(ctx, fileStorage);
        var storeContext = new StubStoreContext(store.Id);
        var addressService = new AddressService(ctx);
        var controller = new ShopStoreController(storeService, employeeService, addressService, storeContext);

        var fileBytes = new byte[] { 1, 2, 3, 4 };
        var formFile = new FormFile(new MemoryStream(fileBytes), 0, fileBytes.Length, "file", "store.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var result = await controller.UploadOwnStoreImage(formFile, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MusicStoreDto>(ok.Value);
        Assert.Equal(store.Id, dto.Id);
        Assert.NotNull(dto.ImagePath);
    }

    [Fact]
    public async Task UploadOwnStoreImage_ThrowsAuthorizationException_WhenCallerIsNotManager()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Store Alpha", "08:00-16:00");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();

        var regularEmployee = new MusicStoreEmployee(appUserId: 20, musicStoreId: store.Id, isManager: false);
        ctx.Set<MusicStoreEmployee>().Add(regularEmployee);
        await ctx.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 20);
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, null!);
        var storeService = new MusicStoreService(ctx, new StubFileStorageService());
        var storeContext = new StubStoreContext(store.Id);
        var addressService = new AddressService(ctx);
        var controller = new ShopStoreController(storeService, employeeService, addressService, storeContext);

        var fileBytes = new byte[] { 1, 2, 3, 4 };
        var formFile = new FormFile(new MemoryStream(fileBytes), 0, fileBytes.Length, "file", "store.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var ex = await Assert.ThrowsAsync<AuthorizationException>(() =>
            controller.UploadOwnStoreImage(formFile, CancellationToken.None));
        Assert.Equal(Messages.ManagerRoleRequired, ex.Message);
    }

    [Fact]
    public async Task UploadOwnStoreImage_ReturnsBadRequest_WhenFileIsNull()
    {
        await using var ctx = TestDbContextFactory.CreateContext(Now);
        var store = new MusicStore("Store Alpha", "08:00-16:00");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();

        var manager = new MusicStoreEmployee(appUserId: 10, musicStoreId: store.Id, isManager: true);
        ctx.Set<MusicStoreEmployee>().Add(manager);
        await ctx.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 10);
        var employeeService = new ShopEmployeeService(ctx, new StubUserIdentityService(), actor, null!);
        var storeService = new MusicStoreService(ctx, new StubFileStorageService());
        var storeContext = new StubStoreContext(store.Id);
        var addressService = new AddressService(ctx);
        var controller = new ShopStoreController(storeService, employeeService, addressService, storeContext);

        var result = await controller.UploadOwnStoreImage(null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequest.Value);
    }

    private sealed class StubStoreContext(int storeId) : IStoreContext
    {
        public Task<int> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(storeId);
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
}
