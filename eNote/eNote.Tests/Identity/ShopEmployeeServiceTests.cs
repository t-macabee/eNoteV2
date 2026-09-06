using eNote.API.Controllers.Admin;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Paging;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Employees;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Identity;
using eNote.Domain.Entities.Rentals;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Mvc;

namespace eNote.Tests.Identity;

public sealed class ShopEmployeeServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPagedAsync_ReturnsEmployeesAcrossAllStores_WithStoreInfo()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);

        var store1 = new MusicStore("Store Alpha", "08:00-16:00");
        var store2 = new MusicStore("Store Beta", "09:00-17:00");
        context.Set<MusicStore>().AddRange(store1, store2);
        await context.SaveChangesAsync();

        var emp1 = new MusicStoreEmployee(appUserId: 101, musicStoreId: store1.Id, isManager: true);
        var emp2 = new MusicStoreEmployee(appUserId: 102, musicStoreId: store2.Id, isManager: false);
        context.Set<MusicStoreEmployee>().AddRange(emp1, emp2);
        await context.SaveChangesAsync();

        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [101] = StubUserIdentityService.User(101, "alpha_emp", "Alice", "Alpha"),
            [102] = StubUserIdentityService.User(102, "beta_emp", "Bob", "Beta")
        });

        var service = new ShopEmployeeService(context, identity, new StubCurrentActor());

        var result = await service.GetPagedAsync(new ShopEmployeeSearchObject { IncludeTotalCount = true });

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);

        var alice = result.Items.Single(x => x.AppUserId == 101);
        Assert.Equal("Alice", alice.FirstName);
        Assert.Equal("Alpha", alice.LastName);
        Assert.Equal("alpha_emp", alice.Username);
        Assert.Equal("Store Alpha", alice.StoreName);
        Assert.Equal("Store Alpha", alice.MusicStoreName);
        Assert.Equal(store1.Id, alice.MusicStoreId);
        Assert.True(alice.IsManager);
        Assert.True(alice.IsActive);

        var bob = result.Items.Single(x => x.AppUserId == 102);
        Assert.Equal("Bob", bob.FirstName);
        Assert.Equal("Beta", bob.LastName);
        Assert.Equal("beta_emp", bob.Username);
        Assert.Equal("Store Beta", bob.StoreName);
        Assert.Equal("Store Beta", bob.MusicStoreName);
        Assert.Equal(store2.Id, bob.MusicStoreId);
        Assert.False(bob.IsManager);
        Assert.True(bob.IsActive);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByNameAndUsername()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);

        var store = new MusicStore("Central Music", "09:00-17:00");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        context.Set<MusicStoreEmployee>().AddRange(
            new MusicStoreEmployee(1, store.Id, false),
            new MusicStoreEmployee(2, store.Id, false),
            new MusicStoreEmployee(3, store.Id, false));
        await context.SaveChangesAsync();

        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [1] = StubUserIdentityService.User(1, "jdoe", "Jane", "Doe"),
            [2] = StubUserIdentityService.User(2, "asmith", "Alice", "Smith"),
            [3] = StubUserIdentityService.User(3, "brown", "Bob", "Brown")
        });

        var service = new ShopEmployeeService(context, identity, new StubCurrentActor());

        var result = await service.GetPagedAsync(new ShopEmployeeSearchObject { Name = "Smith" });

        var dto = Assert.Single(result.Items);
        Assert.Equal("Alice", dto.FirstName);
        Assert.Equal("Smith", dto.LastName);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByMusicStoreId()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);

        var store1 = new MusicStore("Store 1", "09-17");
        var store2 = new MusicStore("Store 2", "09-17");
        context.Set<MusicStore>().AddRange(store1, store2);
        await context.SaveChangesAsync();

        context.Set<MusicStoreEmployee>().AddRange(
            new MusicStoreEmployee(1, store1.Id, false),
            new MusicStoreEmployee(2, store2.Id, false));
        await context.SaveChangesAsync();

        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [1] = StubUserIdentityService.User(1, "emp1", "Emp", "One"),
            [2] = StubUserIdentityService.User(2, "emp2", "Emp", "Two")
        });

        var service = new ShopEmployeeService(context, identity, new StubCurrentActor());

        var result = await service.GetPagedAsync(new ShopEmployeeSearchObject { MusicStoreId = store2.Id });

        var dto = Assert.Single(result.Items);
        Assert.Equal(store2.Id, dto.MusicStoreId);
        Assert.Equal("Store 2", dto.StoreName);
    }

    [Fact]
    public async Task GetPagedAsync_RespectsPaging()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);

        var store = new MusicStore("Store", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        context.Set<MusicStoreEmployee>().AddRange(
            new MusicStoreEmployee(1, store.Id, false),
            new MusicStoreEmployee(2, store.Id, false),
            new MusicStoreEmployee(3, store.Id, false));
        await context.SaveChangesAsync();

        var identity = new StubUserIdentityService();
        var service = new ShopEmployeeService(context, identity, new StubCurrentActor());

        var result = await service.GetPagedAsync(new ShopEmployeeSearchObject { Page = 2, PageSize = 1 });

        var dto = Assert.Single(result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, dto.AppUserId);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedEmployee_WithStoreName()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);

        var store = new MusicStore("Guitar Shop", "10:00-18:00");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var emp = new MusicStoreEmployee(42, store.Id, true);
        context.Set<MusicStoreEmployee>().Add(emp);
        await context.SaveChangesAsync();

        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [42] = StubUserIdentityService.User(42, "jdoe", "Jane", "Doe")
        });

        var service = new ShopEmployeeService(context, identity, new StubCurrentActor());

        var dto = await service.GetByIdAsync(emp.Id);

        Assert.Equal(emp.Id, dto.Id);
        Assert.Equal(42, dto.AppUserId);
        Assert.Equal("Jane", dto.FirstName);
        Assert.Equal("jdoe", dto.Username);
        Assert.Equal("Guitar Shop", dto.StoreName);
        Assert.Equal("Guitar Shop", dto.MusicStoreName);
        Assert.Equal(store.Id, dto.MusicStoreId);
        Assert.True(dto.IsManager);
    }

    [Fact]
    public async Task GetByIdAsync_CanLookupByAppUserId()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);

        var store = new MusicStore("Drum Shop", "10:00-18:00");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var emp = new MusicStoreEmployee(99, store.Id, false);
        context.Set<MusicStoreEmployee>().Add(emp);
        await context.SaveChangesAsync();

        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [99] = StubUserIdentityService.User(99, "drummer", "Dave", "Drummer")
        });

        var service = new ShopEmployeeService(context, identity, new StubCurrentActor());

        var dto = await service.GetByIdAsync(99);

        Assert.Equal(99, dto.AppUserId);
        Assert.Equal("Drum Shop", dto.StoreName);
    }

    [Fact]
    public async Task GetByIdAsync_Throws_WhenEmployeeMissing()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var service = new ShopEmployeeService(context, new StubUserIdentityService(), new StubCurrentActor());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(9999));
    }

    [Fact]
    public async Task GetPagedAsync_ReflectsDeactivatedUser()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);

        var store = new MusicStore("Store", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var emp = new MusicStoreEmployee(50, store.Id, false);
        context.Set<MusicStoreEmployee>().Add(emp);
        await context.SaveChangesAsync();

        var deactivatedUser = new UserIdentityDto
        {
            Id = 50,
            Username = "deactivated_user",
            FirstName = "Inactive",
            LastName = "Person",
            IsActive = false
        };

        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [50] = deactivatedUser
        });

        var service = new ShopEmployeeService(context, identity, new StubCurrentActor());

        var result = await service.GetPagedAsync(new ShopEmployeeSearchObject());

        var dto = Assert.Single(result.Items);
        Assert.False(dto.IsActive);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByIsActive_ReflectsIdentityUserActiveState()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);

        var store = new MusicStore("Store", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var activeEmp = new MusicStoreEmployee(50, store.Id, false);
        var deactivatedEmp = new MusicStoreEmployee(51, store.Id, false);
        context.Set<MusicStoreEmployee>().AddRange(activeEmp, deactivatedEmp);
        await context.SaveChangesAsync();

        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [50] = new() { Id = 50, Username = "active_user", FirstName = "Active", LastName = "User", IsActive = true },
            [51] = new() { Id = 51, Username = "deactivated_user", FirstName = "Inactive", LastName = "User", IsActive = false }
        });

        var service = new ShopEmployeeService(context, identity, new StubCurrentActor());

        var activeResult = await service.GetPagedAsync(new ShopEmployeeSearchObject { IsActive = true });
        Assert.DoesNotContain(activeResult.Items, x => x.AppUserId == 51);
        Assert.Contains(activeResult.Items, x => x.AppUserId == 50);

        var inactiveResult = await service.GetPagedAsync(new ShopEmployeeSearchObject { IsActive = false });
        Assert.Contains(inactiveResult.Items, x => x.AppUserId == 51);
        Assert.DoesNotContain(inactiveResult.Items, x => x.AppUserId == 50);
    }

    [Fact]
    public async Task AdminEmployeeController_GetPaged_And_GetById_ReturnOk()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);

        var store = new MusicStore("Keyboard Store", "09:00-17:00");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var emp = new MusicStoreEmployee(77, store.Id, false);
        context.Set<MusicStoreEmployee>().Add(emp);
        await context.SaveChangesAsync();

        var identity = new StubUserIdentityService(new Dictionary<int, UserIdentityDto>
        {
            [77] = StubUserIdentityService.User(77, "keyboardist", "Ken", "Keys")
        });

        var service = new ShopEmployeeService(context, identity, new StubCurrentActor());
        var controller = new AdminEmployeeController(service);

        var pagedResult = await controller.GetPaged(new ShopEmployeeSearchObject(), CancellationToken.None);
        var okPaged = Assert.IsType<OkObjectResult>(pagedResult.Result);
        var pagedData = Assert.IsType<PagedResult<ShopEmployeeDto>>(okPaged.Value);
        Assert.Single(pagedData.Items);

        var itemResult = await controller.GetById(emp.Id, CancellationToken.None);
        var okItem = Assert.IsType<OkObjectResult>(itemResult.Result);
        var itemData = Assert.IsType<ShopEmployeeDto>(okItem.Value);
        Assert.Equal("Keyboard Store", itemData.StoreName);
    }
}
