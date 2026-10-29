using eNote.API.Controllers.Admin;
using eNote.Application.Common.Paging;
using eNote.Application.Features.Identity.Employees;
using eNote.Application.Features.Identity.Users;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Mvc;

namespace eNote.Tests.Identity;

public sealed class AdminEmployeeControllerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPaged_And_GetById_ReturnOk()
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

        var service = new ShopEmployeeService(context, identity, new StubCurrentActor(), null!);
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
