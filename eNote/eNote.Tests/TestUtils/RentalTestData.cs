using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace eNote.Tests.TestUtils;

public static class RentalTestData
{
    public static ENoteContext CreateContext(DateTime now)
    {
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ENoteContext(options, new FixedClock(now), new StubCurrentActor(storeId: 1)) { ExplicitStoreId = 1 };
    }

    public static async Task<Instrument> SeedInstrumentAsync(ENoteContext context)
    {
        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        context.Set<InstrumentType>().Add(type);
        await context.SaveChangesAsync();

        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var instrument = new Instrument("Stradivarius", "Yamaha", null, null, type.Id, store.Id);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();
        return instrument;
    }

    public static InstrumentRental CreateCompletedRental(Instrument instrument, int studentId, DateTime now)
    {
        var rental = new InstrumentRental(instrument.Id, studentId, instrument.MusicStoreId, now.AddDays(-10), null);
        rental.Approve(50m, null, now.AddDays(-9), 1);
        rental.Pickup(now.AddDays(-9));
        rental.Complete(now.AddDays(-3), null);
        return rental;
    }
}
