using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Features.Rentals.Instruments.Services;
using eNote.Application.Features.Rentals.Recommendations.Services;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace eNote.Tests.InstrumentRentals;

public sealed class TenantIsolationTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPagedForStoreAsync_ExcludesOtherStoreRentals()
    {
        var dbName = Guid.NewGuid().ToString();

        await using var context = CreateContext(storeId: 1, dbName);

        var (store1Id, store2Id, store1RentalId, store2RentalId) = await SeedTwoStoresWithRentalsAsync(context);

        var service1 = CreateRentalQueryService(context, storeId: store1Id);
        var result1 = await service1.GetPagedForStoreAsync(new InstrumentRentalSearchObject { PageSize = 10 });

        Assert.Single(result1.Items);
        Assert.Equal(store1RentalId, result1.Items[0].Id);

        await using var context2 = CreateContext(storeId: store2Id, dbName);

        var service2 = CreateRentalQueryService(context2, storeId: store2Id);
        var result2 = await service2.GetPagedForStoreAsync(new InstrumentRentalSearchObject { PageSize = 10 });

        Assert.Single(result2.Items);
        Assert.Equal(store2RentalId, result2.Items[0].Id);
    }

    [Fact]
    public async Task GetByIdForStoreAsync_Throws_WhenRentalBelongsToOtherStore()
    {
        await using var context = CreateContext(storeId: 1);

        var (_, _, _, store2RentalId) = await SeedTwoStoresWithRentalsAsync(context);
        var service = CreateRentalQueryService(context, storeId: 1);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForStoreAsync(store2RentalId));
    }

    [Fact]
    public async Task GetByIdAsync_Throws_WhenInstrumentBelongsToOtherStore()
    {
        await using var context = CreateContext(storeId: 1);

        var store1 = await SeedStoreAsync(context, "Store 1");
        var store2 = await SeedStoreAsync(context, "Store 2");
        var instr1 = await SeedInstrumentAsync(context, store1.Id);
        var instr2 = await SeedInstrumentAsync(context, store2.Id);
        var service = CreateInstrumentService(context, storeId: store1.Id);

        var dto1 = await service.GetByIdAsync(instr1.Id);

        Assert.NotNull(dto1);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(instr2.Id));
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenInstrumentBelongsToOtherStore()
    {
        await using var context = CreateContext(storeId: 1);

        var store1 = await SeedStoreAsync(context, "Store 1");
        var store2 = await SeedStoreAsync(context, "Store 2");
        var instr2 = await SeedInstrumentAsync(context, store2.Id);
        var service = CreateInstrumentService(context, storeId: store1.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(instr2.Id, new InstrumentUpdateRequest()));
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenInstrumentBelongsToOtherStore()
    {
        await using var context = CreateContext(storeId: 1);

        var store1 = await SeedStoreAsync(context, "Store 1");
        var store2 = await SeedStoreAsync(context, "Store 2");
        var instr2 = await SeedInstrumentAsync(context, store2.Id);
        var service = CreateInstrumentService(context, storeId: store1.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(instr2.Id));
    }

    [Fact]
    public async Task GetPagedAsync_ExcludesOtherStoreInstruments()
    {
        await using var context = CreateContext(storeId: 1);

        var store1 = await SeedStoreAsync(context, "Store A");
        var store2 = await SeedStoreAsync(context, "Store B");

        await SeedInstrumentAsync(context, store1.Id);
        await SeedInstrumentAsync(context, store2.Id);

        var service = CreateInstrumentService(context, storeId: store1.Id);
        var result = await service.GetPagedAsync(new InstrumentSearchObject { PageSize = 10 });

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task RecordInstrumentViewAsync_ThrowsWhenInstrumentBelongsToOtherStore()
    {
        await using var context = CreateContext(storeId: 1);

        var store1 = await SeedStoreAsync(context, "Store A");
        var store2 = await SeedStoreAsync(context, "Store B");
        var instr2 = await SeedInstrumentAsync(context, store2.Id);

        var service = new RecommendationService(context, TestMapper.Create(), new StubCurrentActor(storeId: store1.Id), new FixedClock(Now));
        await Assert.ThrowsAsync<NotFoundException>(() => service.RecordInstrumentViewAsync(instr2.Id));
    }

    private static ENoteContext CreateContext(int storeId, string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ENoteContext(options, new FixedClock(Now), new StubCurrentActor(storeId: storeId));
    }

    private static async Task<(int Store1Id, int Store2Id, int Rental1Id, int Rental2Id)> SeedTwoStoresWithRentalsAsync(ENoteContext context)
    {
        var store1 = await SeedStoreAsync(context, "Music Shop A");
        var store2 = await SeedStoreAsync(context, "Music Shop B");
        var instr1 = await SeedInstrumentAsync(context, store1.Id);
        var instr2 = await SeedInstrumentAsync(context, store2.Id);
        var student = new Student(appUserId: 100, enrollmentDate: Now.AddMonths(-1));

        student.UpdateMembership(Now.AddDays(1));
        context.Set<Student>().Add(student);

        await context.SaveChangesAsync();
        var rental1 = new InstrumentRental(instr1.Id, student.Id, store1.Id, Now, null);

        context.Set<InstrumentRental>().Add(rental1);
        var rental2 = new InstrumentRental(instr2.Id, student.Id, store2.Id, Now, null);

        context.Set<InstrumentRental>().Add(rental2);
        await context.SaveChangesAsync();

        return (store1.Id, store2.Id, rental1.Id, rental2.Id);
    }

    private static async Task<MusicStore> SeedStoreAsync(ENoteContext context, string name)
    {
        var store = new MusicStore(name, "09-17");

        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();
        return store;
    }

    private static async Task<Instrument> SeedInstrumentAsync(ENoteContext context, int storeId)
    {
        if (!await context.Set<InstrumentType>().AnyAsync())
        {
            context.Set<InstrumentType>().Add(new InstrumentType { Type = "Guitar", MonthlyFee = 50m });
            await context.SaveChangesAsync();
        }
        var instrument = new Instrument("Stradivarius", "Yamaha", null, null, 1, storeId);

        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();
        return instrument;
    }

    private static RentalQueryService CreateRentalQueryService(ENoteContext context, int storeId) => new(context, TestMapper.Create(), new StubCurrentActor(storeId: storeId), new FixedClock(Now));

    private static InstrumentService CreateInstrumentService(ENoteContext context, int storeId) =>
        new(context, TestMapper.Create(), new StubCurrentActor(storeId: storeId, employee: new MusicStoreEmployee(appUserId: 1, musicStoreId: storeId, isManager: false)), new StubFileStorageService());
}
