using eNote.Application.Features.Rentals.Recommendations.Services;
using eNote.Domain.Entities.Rentals;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace eNote.Tests.InstrumentRentals;

public sealed class RecommendationServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ReturnsEmpty_WhenNoCandidates()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context);
        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync(5);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_ExcludesInstrumentsWithActiveRentals()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context);
        var instrument = await SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-10), null);
        rental.Approve(50m, null, Now.AddDays(-10), 1);
        rental.Pickup(Now.AddDays(-5));
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync(5);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_CompletedRentalsDoNotExcludeInstrument()
    {
        // Completed rentals are NOT actively rented — the instrument should remain eligible
        // for recommendation (student may want to re-rent it).
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context);
        var instrument = await SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-30), null);
        rental.Approve(50m, null, Now.AddDays(-30), 1);
        rental.Pickup(Now.AddDays(-25));
        rental.Complete(Now.AddDays(-1), null);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync(5);

        Assert.NotEmpty(result);
        Assert.Contains(result, r => r.Instrument.Id == instrument.Id);
    }

    [Fact]
    public async Task GetRecommendedInstrumentsAsync_HigherScoredInstrumentRanksFirst()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context);
        var (typeA, typeB) = await SeedTwoTypesAsync(context);
        var store = await SeedStoreAsync(context);

        // instrumentA: preferred type + global popularity; instrumentB: unknown type
        var instrumentA = new Instrument("InstrA", "YAM", null, null, typeA.Id, store.Id);
        var instrumentB = new Instrument("InstrB", "ROL", null, null, typeB.Id, store.Id);
        context.Set<Instrument>().AddRange(instrumentA, instrumentB);
        await context.SaveChangesAsync();

        var r1 = new InstrumentRental(instrumentA.Id, student.Id, store.Id, Now.AddDays(-60), null);
        r1.Approve(50m, null, Now.AddDays(-60), 1);
        r1.Pickup(Now.AddDays(-55));
        r1.Complete(Now.AddDays(-30), null);
        context.Set<InstrumentRental>().Add(r1);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);

        var result = await service.GetRecommendedInstrumentsAsync(5);

        Assert.NotEmpty(result);
        Assert.Equal(instrumentA.Id, result[0].Instrument.Id);
    }

    private static ENoteContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ENoteContext(options, new FixedClock(Now), new StubCurrentActor(storeId: 1));
    }

    private static async Task<Student> SeedStudentAsync(ENoteContext context)
    {
        var student = new Student(appUserId: 100, Now.AddMonths(-1));
        student.UpdateMembership(Now.AddDays(1));
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        return student;
    }

    private static async Task<Instrument> SeedInstrumentAsync(ENoteContext context)
    {
        context.Set<InstrumentType>().Add(new InstrumentType { Type = "Guitar", MonthlyFee = 50m });
        await context.SaveChangesAsync();
        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();
        var instrument = new Instrument("Stradivarius", "Yamaha", null, null, 1, store.Id);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();
        return instrument;
    }

    private static async Task<(InstrumentType TypeA, InstrumentType TypeB)> SeedTwoTypesAsync(ENoteContext context)
    {
        var typeA = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        var typeB = new InstrumentType { Type = "Piano", MonthlyFee = 80m };
        context.Set<InstrumentType>().AddRange(typeA, typeB);
        await context.SaveChangesAsync();
        return (typeA, typeB);
    }

    private static async Task<MusicStore> SeedStoreAsync(ENoteContext context)
    {
        var store = new MusicStore("Music Shop", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();
        return store;
    }

    private static RecommendationService CreateService(ENoteContext context, Student student) =>
        new(context, TestMapper.Create(), new StubCurrentActor(student: student, storeId: 1), new FixedClock(Now));
}
