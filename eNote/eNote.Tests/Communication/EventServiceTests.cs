using eNote.Application.Features.Communication.Events;
using eNote.Domain.Entities.Communication;
using eNote.Domain.Entities.Shared;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

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
        Assert.NotEqual(0, dto.Id);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedById_ToCurrentUser()
    {
        var ctx = TestDbContextFactory.CreateContext(Now, currentUser: new StubCurrentActor(userId: 77));
        var service = new EventService(ctx);

        var dto = await service.CreateAsync(new EventRequest
        {
            Title = "Authored",
            Description = "Desc",
            StartsAt = Now.AddDays(1)
        });

        var entity = await ctx.Set<Event>().SingleAsync(e => e.Id == dto.Id);
        Assert.Equal(77, entity.CreatedById);
        Assert.Null(entity.UpdatedById);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedById_ToCurrentUser()
    {
        var ctx = TestDbContextFactory.CreateContext(Now, currentUser: new StubCurrentActor(userId: 77));
        var service = new EventService(ctx);

        var created = await service.CreateAsync(new EventRequest
        {
            Title = "Original",
            Description = "Desc",
            StartsAt = Now.AddDays(1)
        });

        await service.UpdateAsync(created.Id, new EventRequest
        {
            Title = "Updated",
            Description = "Desc",
            StartsAt = Now.AddDays(2)
        });

        var entity = await ctx.Set<Event>().SingleAsync(e => e.Id == created.Id);
        Assert.Equal(77, entity.UpdatedById);
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
    public async Task GetPagedAsync_FiltersByTitle()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new EventService(ctx);

        await service.CreateAsync(new EventRequest { Title = "Concert A", Description = "D", StartsAt = Now.AddDays(1) });
        await service.CreateAsync(new EventRequest { Title = "Concert B", Description = "D", StartsAt = Now.AddDays(2) });
        await service.CreateAsync(new EventRequest { Title = "Workshop", Description = "D", StartsAt = Now.AddDays(3) });

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
