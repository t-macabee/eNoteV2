using eNote.API.Controllers.Admin;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Features.Communication.Events;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Mvc;

namespace eNote.Tests.Communication;

public sealed class AdminEventControllerTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Create_WithCourseId_ThrowsBusinessException()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var controller = new AdminEventController(new EventService(ctx));

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            controller.Create(new EventRequest
            {
                Title = "Recital",
                Description = "Course recital",
                StartsAt = Now.AddDays(1),
                CourseId = 10
            }, CancellationToken.None));

        Assert.Equal(Messages.AdminEventPlatformWideOnly, ex.Message);
    }

    [Fact]
    public async Task Create_WithInstructorId_ThrowsBusinessException()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var controller = new AdminEventController(new EventService(ctx));

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            controller.Create(new EventRequest
            {
                Title = "Recital",
                Description = "Instructor recital",
                StartsAt = Now.AddDays(1),
                InstructorId = 5
            }, CancellationToken.None));

        Assert.Equal(Messages.AdminEventPlatformWideOnly, ex.Message);
    }

    [Fact]
    public async Task Create_PlatformWide_Succeeds()
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
        Assert.Null(dto.CourseId);
        Assert.Null(dto.InstructorId);
    }

    [Fact]
    public async Task Update_WithCourseId_ThrowsBusinessException()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new EventService(ctx);
        var existing = await service.CreateAsync(new EventRequest
        {
            Title = "Existing",
            Description = "Desc",
            StartsAt = Now.AddDays(1)
        });
        var controller = new AdminEventController(service);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            controller.Update(existing.Id, new EventRequest
            {
                Title = "Updated",
                Description = "Desc",
                StartsAt = Now.AddDays(1),
                CourseId = 2
            }, CancellationToken.None));

        Assert.Equal(Messages.AdminEventPlatformWideOnly, ex.Message);
    }

    [Fact]
    public async Task Update_ExistingCourseTiedEvent_ThrowsBusinessException()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);
        var service = new EventService(ctx);
        var existing = await service.CreateAsync(new EventRequest
        {
            Title = "Course Event",
            Description = "Desc",
            StartsAt = Now.AddDays(1),
            CourseId = harness.Course.Id
        });
        var controller = new AdminEventController(service);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            controller.Update(existing.Id, new EventRequest
            {
                Title = "Platform update attempt",
                Description = "Desc",
                StartsAt = Now.AddDays(1)
            }, CancellationToken.None));

        Assert.Equal(Messages.AdminEventPlatformWideOnly, ex.Message);
    }

    [Fact]
    public async Task Delete_ExistingInstructorTiedEvent_ThrowsBusinessException()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);
        var service = new EventService(ctx);
        var existing = await service.CreateAsync(new EventRequest
        {
            Title = "Instructor Event",
            Description = "Desc",
            StartsAt = Now.AddDays(1),
            InstructorId = harness.Instructor.Id
        });
        var controller = new AdminEventController(service);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            controller.Delete(existing.Id, CancellationToken.None));

        Assert.Equal(Messages.AdminEventPlatformWideOnly, ex.Message);
    }

    [Fact]
    public async Task Delete_PlatformWideEvent_Succeeds()
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
