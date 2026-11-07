using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Features.Rentals.Instruments.Services;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class StoreBoundarySqlServerTests(SqlServerFixture fixture)
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPagedAsync_AfterStoreResolution_ListsOnlyCallersStore()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var storeA = new MusicStore("Store A", "09:00-17:00");
        var storeB = new MusicStore("Store B", "09:00-17:00");
        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        var user = new AppUser { UserName = "employee", Email = "employee@example.com", IsActive = true, FirstName = "Test", LastName = "User" };
        context.Set<MusicStore>().AddRange(storeA, storeB);
        context.Set<InstrumentType>().Add(type);
        context.Set<AppUser>().Add(user);
        await context.SaveChangesAsync();

        var employee = new MusicStoreEmployee(appUserId: user.Id, musicStoreId: storeA.Id, isManager: true);
        context.Set<MusicStoreEmployee>().Add(employee);
        context.Set<Instrument>().AddRange(
            new Instrument("Store A Model", TestManufacturers.Id("Maker A"), null, null, type.Id, storeA.Id),
            new Instrument("Store B Model", TestManufacturers.Id("Maker B"), null, null, type.Id, storeB.Id));
        await context.SaveChangesAsync();

        var storeId = await context.GetCurrentStoreIdAsync();

        Assert.Equal(storeA.Id, storeId);

        var actor = new StubCurrentActor(userId: 1, employee: employee, storeId: storeA.Id);
        var rentalCommands = new RentalCommandService(
            context,
            TestMapper.Create(),
            new FixedClock(Now),
            actor,
            actor,
            actor,
            new RecordingNotificationDispatcher(),
            new StubDisplayNameService(),
            new RentalOptions { OverduePenaltyMultiplier = 0.5m });
        var service = new InstrumentService(context, actor, new StubFileStorageService(), rentalCommands);

        var paged = await service.GetPagedAsync(new InstrumentSearchObject { PageSize = 10 });

        var item = Assert.Single(paged.Items);
        Assert.Equal("Store A Model", item.Model);
    }
}
