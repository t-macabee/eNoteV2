using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Features.Rentals.Instruments.Services;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace eNote.Tests.Rentals;

public sealed class InstrumentServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPagedAsync_FiltersByMusicStoreId()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store1 = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var store2 = await SeedStoreAsync(ctx, "Music Shop Mostar");

        await SeedInstrumentAsync(ctx, store1.Id, type.Id, "Stratocaster", "Fender");
        await SeedInstrumentAsync(ctx, store1.Id, type.Id, "Telecaster", "Fender");
        await SeedInstrumentAsync(ctx, store2.Id, type.Id, "Les Paul", "Gibson");

        var service = CreateInstrumentService(ctx);

        var filteredStore1 = await service.GetPagedAsync(new InstrumentSearchObject { MusicStoreId = store1.Id }, publicView: true);
        Assert.Equal(2, filteredStore1.Items.Count);
        Assert.All(filteredStore1.Items, x => Assert.Equal("Music Shop Sarajevo", x.MusicStore));

        var filteredStore2 = await service.GetPagedAsync(new InstrumentSearchObject { MusicStoreId = store2.Id }, publicView: true);
        Assert.Single(filteredStore2.Items);
        Assert.Equal("Les Paul", filteredStore2.Items[0].Model);
        Assert.Equal("Music Shop Mostar", filteredStore2.Items[0].MusicStore);

        var noFilter = await service.GetPagedAsync(new InstrumentSearchObject(), publicView: true);
        Assert.Equal(3, noFilter.Items.Count);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByModelAndMusicStoreId_Combined()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store1 = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var store2 = await SeedStoreAsync(ctx, "Music Shop Mostar");

        await SeedInstrumentAsync(ctx, store1.Id, type.Id, "Yamaha Pacifica", "Yamaha");
        await SeedInstrumentAsync(ctx, store1.Id, type.Id, "Fender Stratocaster", "Fender");
        await SeedInstrumentAsync(ctx, store2.Id, type.Id, "Yamaha Pacifica", "Yamaha");

        var service = CreateInstrumentService(ctx);

        var filtered = await service.GetPagedAsync(new InstrumentSearchObject
        {
            Model = "Pacifica",
            MusicStoreId = store1.Id
        }, publicView: true);

        Assert.Single(filtered.Items);
        Assert.Equal("Music Shop Sarajevo", filtered.Items[0].MusicStore);
        Assert.Equal("Yamaha Pacifica", filtered.Items[0].Model);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersBySearch_MatchesModelOrManufacturer()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");

        await SeedInstrumentAsync(ctx, store.Id, type.Id, "Pacifica 112V", "Yamaha");
        await SeedInstrumentAsync(ctx, store.Id, type.Id, "Stratocaster", "Fender");
        await SeedInstrumentAsync(ctx, store.Id, type.Id, "Les Paul", "Gibson");

        var service = CreateInstrumentService(ctx);

        // Search matches Model
        var modelMatch = await service.GetPagedAsync(new InstrumentSearchObject
        {
            Search = "Stratocaster"
        }, publicView: true);
        Assert.Single(modelMatch.Items);
        Assert.Equal("Stratocaster", modelMatch.Items[0].Model);

        // Search matches Manufacturer
        var mfrMatch = await service.GetPagedAsync(new InstrumentSearchObject
        {
            Search = "Yamaha"
        }, publicView: true);
        Assert.Single(mfrMatch.Items);
        Assert.Equal("Pacifica 112V", mfrMatch.Items[0].Model);
        Assert.Equal("Yamaha", mfrMatch.Items[0].Manufacturer);
    }


    private static ENoteContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ENoteContext(options, new FixedClock(Now), new StubCurrentActor());
    }

    private static InstrumentService CreateInstrumentService(ENoteContext context) =>
        new(context, TestMapper.Create(), new StubCurrentActor(), new StubFileStorageService());

    private static async Task<MusicStore> SeedStoreAsync(ENoteContext ctx, string name)
    {
        var store = new MusicStore(name, "09-17");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();
        return store;
    }

    private static async Task<InstrumentType> SeedInstrumentTypeAsync(ENoteContext ctx, string typeName)
    {
        var type = new InstrumentType { Type = typeName, MonthlyFee = 50m };
        ctx.Set<InstrumentType>().Add(type);
        await ctx.SaveChangesAsync();
        return type;
    }

    private static async Task<Instrument> SeedInstrumentAsync(ENoteContext ctx, int storeId, int typeId, string model, string manufacturer)
    {
        var instrument = new Instrument(model, manufacturer, null, null, typeId, storeId);
        ctx.Set<Instrument>().Add(instrument);
        await ctx.SaveChangesAsync();
        return instrument;
    }
}
