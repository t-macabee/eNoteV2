using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.ReferenceData.Manufacturers;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Rentals;

public sealed class ManufacturerServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsConflict()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.ManufacturerNameUniqueIndex}\"");
        var context = new ThrowingSaveDbContext(ctx, new DbUpdateException("Unique constraint violated.", inner));
        var service = new ManufacturerService(context);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new ManufacturerRequest { Name = "Fender" }));

        Assert.Equal(Messages.ManufacturerAlreadyExists, ex.Message);
    }

    [Fact]
    public async Task CreateAsync_TrimsName()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new ManufacturerService(ctx);

        var dto = await service.CreateAsync(new ManufacturerRequest { Name = "  Fender  " });

        Assert.Equal("Fender", dto.Name);
        var stored = await ctx.Set<Manufacturer>().AsNoTracking().SingleAsync(x => x.Id == dto.Id);
        Assert.Equal("Fender", stored.Name);
    }

    [Fact]
    public async Task DeleteAsync_Blocked_WhenArchivedInstrumentUsesTheManufacturer()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        await SeedManufacturerAsync(ctx, "Fender");
        var manufacturer = await ctx.Set<Manufacturer>().SingleAsync(x => x.Name == "Fender");
        var service = new ManufacturerService(ctx);
        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        var store = new MusicStore("Music Shop", "09-17");
        ctx.Set<InstrumentType>().Add(type);
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();

        var instrument = new Instrument("Stratocaster", manufacturer.Id, null, null, type.Id, store.Id);
        ctx.Set<Instrument>().Add(instrument);
        await ctx.SaveChangesAsync();

        instrument.SoftDelete();
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(manufacturer.Id));

        Assert.Equal(Messages.ManufacturerDeleteBlocked, ex.Message);
        Assert.True(await ctx.Set<Manufacturer>().AnyAsync(x => x.Id == manufacturer.Id));
    }

    [Fact]
    public async Task DeleteAsync_RemovesUnusedManufacturer()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new ManufacturerService(ctx);
        var created = await service.CreateAsync(new ManufacturerRequest { Name = "Fender" });

        await service.DeleteAsync(created.Id);

        Assert.False(await ctx.Set<Manufacturer>().AnyAsync(x => x.Id == created.Id));
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ThrowsNotFoundException()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new ManufacturerService(ctx);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(999));

        Assert.Equal(Messages.ManufacturerNotFound, ex.Message);
    }

    [Fact]
    public async Task GetPagedAsync_SearchesByPartOfName_AndOrdersByName()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new ManufacturerService(ctx);
        await service.CreateAsync(new ManufacturerRequest { Name = "Yamaha" });
        await service.CreateAsync(new ManufacturerRequest { Name = "Fender" });
        await service.CreateAsync(new ManufacturerRequest { Name = "Gibson" });

        var filtered = await service.GetPagedAsync(new ManufacturerSearchObject { Name = "en" });

        Assert.Equal(["Fender"], filtered.Items.Select(x => x.Name));

        var all = await service.GetPagedAsync(new ManufacturerSearchObject());

        Assert.Equal(["Fender", "Gibson", "Yamaha"], all.Items.Select(x => x.Name));
    }

    private static async Task SeedManufacturerAsync(ENoteContext context, string name)
    {
        context.Set<Manufacturer>().Add(new Manufacturer { Name = name });
        await context.SaveChangesAsync();
    }
}
