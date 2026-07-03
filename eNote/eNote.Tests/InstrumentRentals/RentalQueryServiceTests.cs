using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Domain.Entities.Rentals;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace eNote.Tests.InstrumentRentals;

public sealed class RentalQueryServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPagedForStudentAsync_ReturnsOnlyOwnRentals()
    {
        await using var context = CreateContext();
        var student1 = await SeedStudentAsync(context, appUserId: 100);
        var student2 = await SeedStudentAsync(context, appUserId: 200);
        var instrument = await SeedInstrumentAsync(context);
        context.Set<InstrumentRental>().Add(new InstrumentRental(instrument.Id, student1.Id, instrument.MusicStoreId, Now, null));
        context.Set<InstrumentRental>().Add(new InstrumentRental(instrument.Id, student2.Id, instrument.MusicStoreId, Now, null));
        await context.SaveChangesAsync();
        var service = CreateService(context, student1);

        var result = await service.GetPagedForStudentAsync(new InstrumentRentalSearchObject { PageSize = 10 });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(student1.Id, result.Items[0].StudentProfileId);
    }

    [Fact]
    public async Task GetPagedForStoreAsync_ReturnsAllStoreRentals()
    {
        await using var context = CreateContext();
        var student1 = await SeedStudentAsync(context, appUserId: 100);
        var student2 = await SeedStudentAsync(context, appUserId: 200);
        var instrument = await SeedInstrumentAsync(context);
        context.Set<InstrumentRental>().Add(new InstrumentRental(instrument.Id, student1.Id, instrument.MusicStoreId, Now, null));
        context.Set<InstrumentRental>().Add(new InstrumentRental(instrument.Id, student2.Id, instrument.MusicStoreId, Now, null));
        await context.SaveChangesAsync();
        var service = CreateService(context, student1);

        var result = await service.GetPagedForStoreAsync(new InstrumentRentalSearchObject { PageSize = 10 });

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetByIdForStudentAsync_Throws_WhenRentalBelongsToAnotherStudent()
    {
        await using var context = CreateContext();
        var student1 = await SeedStudentAsync(context, appUserId: 100);
        var student2 = await SeedStudentAsync(context, appUserId: 200);
        var instrument = await SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student2.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, student1);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForStudentAsync(rental.Id));
    }

    [Fact]
    public async Task GetByIdForStudentAsync_AppliesBilling_WhenRentalIsActive()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, appUserId: 100);
        var instrument = await SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-35), null);
        rental.Approve(50m, null, Now.AddDays(-35), 1);
        rental.Pickup(Now.AddDays(-30));
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var dto = await service.GetByIdForStudentAsync(rental.Id);

        Assert.Equal(50m, dto.Fee);
        Assert.True(dto.TotalFee > 0);
        Assert.True(dto.MonthsCharged >= 1);
    }

    private static ENoteContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ENoteContext(options, new FixedClock(Now), new StubCurrentActor(storeId: 1));
    }

    private static async Task<Student> SeedStudentAsync(ENoteContext context, int appUserId)
    {
        var student = new Student(appUserId, Now.AddMonths(-1));
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

    private static RentalQueryService CreateService(ENoteContext context, Student student) =>
        new(context, TestMapper.Create(), new StubCurrentActor(student: student, storeId: 1), new FixedClock(Now));
}
