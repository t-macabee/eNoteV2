using eNote.API.Controllers.Admin;
using eNote.Application.Features.Communication.Events;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Mvc;

namespace eNote.Tests.Communication;

public sealed class AdminEventControllerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Create_Succeeds()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var controller = new AdminEventController(new EventService(ctx));

        var actionResult = await controller.Create(new EventRequest
        {
            Title = "Festival",
            Description = "Platform festival",
            StartsAt = Now.AddDays(2),
            EndsAt = Now.AddDays(2).AddHours(3)
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var dto = Assert.IsType<EventDto>(created.Value);
        Assert.Equal("Festival", dto.Title);
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new EventService(ctx);
        var created = await service.CreateAsync(new EventRequest
        {
            Title = "Original",
            Description = "Desc",
            StartsAt = Now.AddDays(1)
        });
        var controller = new AdminEventController(service);

        var actionResult = await controller.Update(created.Id, new EventRequest
        {
            Title = "Renamed",
            Description = "New desc",
            StartsAt = Now.AddDays(3),
            EndsAt = Now.AddDays(3).AddHours(2)
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var dto = Assert.IsType<EventDto>(ok.Value);
        Assert.Equal("Renamed", dto.Title);
        Assert.Equal(Now.AddDays(3), dto.StartsAt);
    }

    [Fact]
    public async Task Delete_RemovesEvent()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new EventService(ctx);
        var existing = await service.CreateAsync(new EventRequest
        {
            Title = "Platform Event",
            Description = "Desc",
            StartsAt = Now.AddDays(1)
        });
        var controller = new AdminEventController(service);

        var result = await controller.Delete(existing.Id, CancellationToken.None);
        Assert.IsType<NoContentResult>(result);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(existing.Id, CancellationToken.None));
    }
}
