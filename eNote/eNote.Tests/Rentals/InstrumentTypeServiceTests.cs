using eNote.Application.Common.Localization;
using eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Rentals;

public sealed class InstrumentTypeServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task DeleteAsync_Blocked_WhenInstrumentSoftDeleted()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new InstrumentTypeService(ctx);
        var created = await service.CreateAsync(new InstrumentTypeRequest { Type = "Guitar", MonthlyFee = 50m });
        var store = new MusicStore("Music Shop", "09-17");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();
        var instrument = new Instrument("Strat", "Fender", null, null, created.Id, store.Id);
        ctx.Set<Instrument>().Add(instrument);
        await ctx.SaveChangesAsync();

        instrument.SoftDelete();
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(created.Id));

        Assert.Equal(Messages.InstrumentTypeDeleteBlocked, ex.Message);
    }
}
