using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Rentals;

public sealed class InstrumentTypeServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_DuplicateType_ThrowsConflict()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.InstrumentTypeTypeUniqueIndex}\"");
        var context = new ThrowingSaveDbContext(ctx, new DbUpdateException("Unique constraint violated.", inner));
        var service = new InstrumentTypeService(context);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new InstrumentTypeRequest { Type = "Gitara", MonthlyFee = 45m }));

        Assert.Equal(Messages.InstrumentTypeAlreadyExists, ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateType_ThrowsConflict()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var seed = new InstrumentTypeService(ctx);
        await seed.CreateAsync(new InstrumentTypeRequest { Type = "Gitara", MonthlyFee = 45m });
        var klavir = await seed.CreateAsync(new InstrumentTypeRequest { Type = "Klavir", MonthlyFee = 60m });
        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.InstrumentTypeTypeUniqueIndex}\"");
        var context = new ThrowingSaveDbContext(ctx, new DbUpdateException("Unique constraint violated.", inner));
        var service = new InstrumentTypeService(context);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(klavir.Id, new InstrumentTypeRequest { Type = "Gitara", MonthlyFee = 60m }));

        Assert.Equal(Messages.InstrumentTypeAlreadyExists, ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_Blocked_WhenInstrumentSoftDeleted()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new InstrumentTypeService(ctx);
        var created = await service.CreateAsync(new InstrumentTypeRequest { Type = "Guitar", MonthlyFee = 50m });
        var store = new MusicStore("Music Shop", "09-17");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();
        var instrument = new Instrument("Strat", TestManufacturers.Id("Fender"), null, null, created.Id, store.Id);
        ctx.Set<Instrument>().Add(instrument);
        await ctx.SaveChangesAsync();

        instrument.SoftDelete();
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(created.Id));

        Assert.Equal(Messages.InstrumentTypeDeleteBlocked, ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_Blocked_WhenAnInstrumentOfAnotherStoreUsesTheType()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new InstrumentTypeService(ctx);
        var created = await service.CreateAsync(new InstrumentTypeRequest { Type = "Guitar", MonthlyFee = 50m });
        var ownStore = new MusicStore("Own Shop", "09-17");
        var otherStore = new MusicStore("Other Shop", "09-17");
        ctx.Set<MusicStore>().AddRange(ownStore, otherStore);
        await ctx.SaveChangesAsync();
        Assert.NotEqual(ctx.ExplicitStoreId, otherStore.Id);
        ctx.Set<Instrument>().Add(new Instrument("Strat", TestManufacturers.Id("Fender"), null, null, created.Id, otherStore.Id));
        await ctx.SaveChangesAsync();

        var dto = await service.GetByIdAsync(created.Id);
        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(created.Id));

        Assert.Equal(Messages.InstrumentTypeDeleteBlocked, dto.DeleteBlockedReason);
        Assert.Equal(Messages.InstrumentTypeDeleteBlocked, ex.Message);
    }

    [Fact]
    public async Task CreateAsync_TrimsName()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new InstrumentTypeService(ctx);

        var dto = await service.CreateAsync(new InstrumentTypeRequest { Type = "  Gitara  ", MonthlyFee = 45m });

        Assert.Equal("Gitara", dto.Type);
        var stored = await ctx.Set<InstrumentType>().AsNoTracking().SingleAsync(x => x.Id == dto.Id);
        Assert.Equal("Gitara", stored.Type);
    }

    [Fact]
    public async Task UpdateAsync_ChangesNameAndFee()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new InstrumentTypeService(ctx);
        var created = await service.CreateAsync(new InstrumentTypeRequest { Type = "Gitara", MonthlyFee = 45m });

        var updated = await service.UpdateAsync(created.Id, new InstrumentTypeRequest { Type = "Bas gitara", MonthlyFee = 60m });

        Assert.Equal("Bas gitara", updated.Type);
        Assert.Equal(60m, updated.MonthlyFee);
        var stored = await ctx.Set<InstrumentType>().AsNoTracking().SingleAsync(x => x.Id == created.Id);
        Assert.Equal("Bas gitara", stored.Type);
        Assert.Equal(60m, stored.MonthlyFee);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ThrowsNotFoundException()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new InstrumentTypeService(ctx);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateAsync(999, new InstrumentTypeRequest { Type = "Gitara", MonthlyFee = 45m }));

        Assert.Equal(Messages.InstrumentTypeNotFound, ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ThrowsNotFoundException()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new InstrumentTypeService(ctx);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(999));

        Assert.Equal(Messages.InstrumentTypeNotFound, ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUnusedType()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new InstrumentTypeService(ctx);
        var created = await service.CreateAsync(new InstrumentTypeRequest { Type = "Gitara", MonthlyFee = 45m });

        await service.DeleteAsync(created.Id);

        Assert.False(await ctx.Set<InstrumentType>().AnyAsync(x => x.Id == created.Id));
    }

    [Fact]
    public async Task DeleteAsync_Blocked_WhenActiveInstrumentExists()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new InstrumentTypeService(ctx);
        var created = await service.CreateAsync(new InstrumentTypeRequest { Type = "Gitara", MonthlyFee = 45m });
        var store = new MusicStore("Music Shop", "09-17");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();
        ctx.Set<Instrument>().Add(new Instrument("Strat", TestManufacturers.Id("Fender"), null, null, created.Id, store.Id));
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(created.Id));

        Assert.Equal(Messages.InstrumentTypeDeleteBlocked, ex.Message);
        Assert.True(await ctx.Set<InstrumentType>().AnyAsync(x => x.Id == created.Id));
    }

    [Fact]
    public async Task GetPagedAsync_SearchesByPartOfName_AndOrdersByName()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new InstrumentTypeService(ctx);
        await service.CreateAsync(new InstrumentTypeRequest { Type = "Gitara", MonthlyFee = 45m });
        await service.CreateAsync(new InstrumentTypeRequest { Type = "Klavir", MonthlyFee = 60m });
        await service.CreateAsync(new InstrumentTypeRequest { Type = "Bas gitara", MonthlyFee = 55m });

        var filtered = await service.GetPagedAsync(new InstrumentTypeSearchObject { Type = "itar" });

        Assert.Equal(["Bas gitara", "Gitara"], filtered.Items.Select(x => x.Type));

        var all = await service.GetPagedAsync(new InstrumentTypeSearchObject());

        Assert.Equal(["Bas gitara", "Gitara", "Klavir"], all.Items.Select(x => x.Type));
    }
}
