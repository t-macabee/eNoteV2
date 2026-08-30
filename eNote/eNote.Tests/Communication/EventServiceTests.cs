using eNote.Application.Features.Communication.Events;
using eNote.Domain.Entities.Shared;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Communication;

public sealed class EventServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_WithoutOptionalFks_Succeeds()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new EventService(ctx);

        var dto = await service.CreateAsync(new EventRequest
        {
            Title = "School Concert",
            Description = "Annual performance",
            StartsAt = Now.AddDays(7),
            EndsAt = Now.AddDays(7).AddHours(2)
        });

        Assert.Equal("School Concert", dto.Title);
        Assert.Null(dto.AddressId);
        Assert.Null(dto.CourseId);
        Assert.Null(dto.InstructorId);
        Assert.NotEqual(0, dto.Id);
    }

    [Fact]
    public async Task CreateAsync_WithCourseOnly_Succeeds()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);
        var service = new EventService(ctx);

        var dto = await service.CreateAsync(new EventRequest
        {
            Title = "Guitar Recital",
            Description = "Course event",
            StartsAt = Now.AddDays(5),
            CourseId = harness.Course.Id
        });

        Assert.Equal(harness.Course.Id, dto.CourseId);
        Assert.Null(dto.InstructorId);
        Assert.Null(dto.AddressId);
    }

    [Fact]
    public async Task CreateAsync_WithInstructorOnly_Succeeds()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);
        var service = new EventService(ctx);

        var dto = await service.CreateAsync(new EventRequest
        {
            Title = "Masterclass",
            Description = "Instructor event",
            StartsAt = Now.AddDays(5),
            InstructorId = harness.Instructor.Id
        });

        Assert.Equal(harness.Instructor.Id, dto.InstructorId);
        Assert.Null(dto.CourseId);
    }

    [Fact]
    public async Task CreateAsync_WithAddressOnly_Succeeds()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var address = await SeedAddressAsync(ctx);
        var service = new EventService(ctx);

        var dto = await service.CreateAsync(new EventRequest
        {
            Title = "Public Show",
            Description = "At venue",
            StartsAt = Now.AddDays(3),
            AddressId = address.Id
        });

        Assert.Equal(address.Id, dto.AddressId);
    }

    [Fact]
    public async Task CreateAsync_WithAllOptionalFks_Succeeds()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);
        var address = await SeedAddressAsync(ctx);
        var service = new EventService(ctx);

        var dto = await service.CreateAsync(new EventRequest
        {
            Title = "Full Event",
            Description = "All contexts",
            StartsAt = Now.AddDays(10),
            EndsAt = Now.AddDays(10).AddHours(3),
            AddressId = address.Id,
            CourseId = harness.Course.Id,
            InstructorId = harness.Instructor.Id
        });

        Assert.Equal(address.Id, dto.AddressId);
        Assert.Equal(harness.Course.Id, dto.CourseId);
        Assert.Equal(harness.Instructor.Id, dto.InstructorId);
        Assert.Equal("Full Event", dto.Title);
    }

    [Fact]
    public async Task CreateAsync_WithBothCourseAndInstructor_Succeeds()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);
        var service = new EventService(ctx);

        var dto = await service.CreateAsync(new EventRequest
        {
            Title = "Course + Instructor",
            Description = "Both",
            StartsAt = Now.AddDays(4),
            CourseId = harness.Course.Id,
            InstructorId = harness.Instructor.Id
        });

        Assert.Equal(harness.Course.Id, dto.CourseId);
        Assert.Equal(harness.Instructor.Id, dto.InstructorId);
    }

    [Fact]
    public async Task CreateAsync_WithoutEndsAt_Succeeds()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new EventService(ctx);

        var dto = await service.CreateAsync(new EventRequest
        {
            Title = "Open End",
            Description = "No end time",
            StartsAt = Now.AddDays(2),
            EndsAt = null
        });

        Assert.Null(dto.EndsAt);
    }

    [Fact]
    public async Task CreateAsync_EndsBeforeStarts_ThrowsBusinessException()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new EventService(ctx);

        await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(new EventRequest
        {
            Title = "Bad Range",
            Description = "Invalid",
            StartsAt = Now.AddDays(2),
            EndsAt = Now.AddDays(1)
        }));
    }

    [Fact]
    public async Task CreateAsync_InvalidAddressId_ThrowsNotFound()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new EventService(ctx);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(new EventRequest
        {
            Title = "Bad Address",
            Description = "Invalid FK",
            StartsAt = Now.AddDays(2),
            AddressId = 9999
        }));
    }

    [Fact]
    public async Task CreateAsync_InvalidCourseId_ThrowsNotFound()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new EventService(ctx);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(new EventRequest
        {
            Title = "Bad Course",
            Description = "Invalid",
            StartsAt = Now.AddDays(2),
            CourseId = 9999
        }));
    }

    [Fact]
    public async Task CreateAsync_InvalidInstructorId_ThrowsNotFound()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new EventService(ctx);

        await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(new EventRequest
        {
            Title = "Bad Instructor",
            Description = "Invalid",
            StartsAt = Now.AddDays(2),
            InstructorId = 9999
        }));
    }

    [Fact]
    public async Task UpdateAsync_ChangesAllFields_IncludingClearingVenue()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var address = await SeedAddressAsync(ctx);
        var service = new EventService(ctx);

        var created = await service.CreateAsync(new EventRequest
        {
            Title = "Original",
            Description = "Desc",
            StartsAt = Now.AddDays(5),
            AddressId = address.Id
        });

        var updated = await service.UpdateAsync(created.Id, new EventRequest
        {
            Title = "Updated",
            Description = "New desc",
            StartsAt = Now.AddDays(6),
            EndsAt = Now.AddDays(6).AddHours(1),
            AddressId = null
        });

        Assert.Equal("Updated", updated.Title);
        Assert.Null(updated.AddressId);
        Assert.Equal(Now.AddDays(6), updated.StartsAt);
    }

    [Fact]
    public async Task UpdateAsync_FillsVenueLater_Succeeds()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var address = await SeedAddressAsync(ctx);
        var service = new EventService(ctx);

        var created = await service.CreateAsync(new EventRequest
        {
            Title = "No Venue Yet",
            Description = "TBD",
            StartsAt = Now.AddDays(5),
            AddressId = null
        });

        var updated = await service.UpdateAsync(created.Id, new EventRequest
        {
            Title = "No Venue Yet",
            Description = "TBD",
            StartsAt = Now.AddDays(5),
            AddressId = address.Id
        });

        Assert.Equal(address.Id, updated.AddressId);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEvent()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new EventService(ctx);

        var dto = await service.CreateAsync(new EventRequest
        {
            Title = "To Delete",
            Description = "Temp",
            StartsAt = Now.AddDays(1)
        });

        await service.DeleteAsync(dto.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(dto.Id));
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_Throws()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new EventService(ctx);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(9999));
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByTitleAndCourse()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(ctx, Now);
        var service = new EventService(ctx);

        await service.CreateAsync(new EventRequest { Title = "Concert A", Description = "D", StartsAt = Now.AddDays(1), CourseId = harness.Course.Id });
        await service.CreateAsync(new EventRequest { Title = "Concert B", Description = "D", StartsAt = Now.AddDays(2) });
        await service.CreateAsync(new EventRequest { Title = "Workshop", Description = "D", StartsAt = Now.AddDays(3) });

        var byCourse = await service.GetPagedAsync(new EventSearchObject { CourseId = harness.Course.Id });
        Assert.Single(byCourse.Items);
        Assert.Equal("Concert A", byCourse.Items[0].Title);

        var byTitle = await service.GetPagedAsync(new EventSearchObject { Title = "Concert" });
        Assert.Equal(2, byTitle.Items.Count);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByDateRange()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new EventService(ctx);

        await service.CreateAsync(new EventRequest { Title = "E1", Description = "D", StartsAt = Now.AddDays(1) });
        await service.CreateAsync(new EventRequest { Title = "E2", Description = "D", StartsAt = Now.AddDays(10) });
        await service.CreateAsync(new EventRequest { Title = "E3", Description = "D", StartsAt = Now.AddDays(20) });

        var range = await service.GetPagedAsync(new EventSearchObject { From = Now.AddDays(5), To = Now.AddDays(15) });
        Assert.Single(range.Items);
        Assert.Equal("E2", range.Items[0].Title);
    }

    private static async Task<Address> SeedAddressAsync(ENoteContext ctx)
    {
        var city = new City { Name = "Test City" };
        ctx.Set<City>().Add(city);
        await ctx.SaveChangesAsync();

        var address = new Address { CityId = city.Id, Street = "Main St", Number = "1" };
        ctx.Set<Address>().Add(address);
        await ctx.SaveChangesAsync();
        return address;
    }
}
