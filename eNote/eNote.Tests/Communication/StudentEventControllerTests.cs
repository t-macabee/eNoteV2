using eNote.API.Controllers.Events;
using eNote.Application.Common.Paging;
using eNote.Application.Features.Communication.Events;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Mvc;

namespace eNote.Tests.Communication;

public sealed class StudentEventControllerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPagedForStudent_ReturnsEveryEvent()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new EventService(ctx);
        await service.CreateAsync(new EventRequest { Title = "Concert", Description = "D", StartsAt = Now.AddDays(1) });
        await service.CreateAsync(new EventRequest { Title = "Workshop", Description = "D", StartsAt = Now.AddDays(2) });
        await service.CreateAsync(new EventRequest { Title = "Gala", Description = "D", StartsAt = Now.AddDays(3) });
        var controller = new StudentEventController(service);

        var actionResult = await controller.GetPagedForStudent(new EventSearchObject(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var page = Assert.IsType<PagedResult<EventDto>>(ok.Value);
        Assert.Equal(3, page.Items.Count);
    }
}
