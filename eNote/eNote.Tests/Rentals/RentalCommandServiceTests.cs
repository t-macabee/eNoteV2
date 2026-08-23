using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace eNote.Tests.Rentals;

public sealed class RentalCommandServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateRequestAsync_Throws_WhenMembershipInactive()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: false);
        var instrument = await SeedInstrumentAsync(context);
        var service = CreateService(context, student);

        await Assert.ThrowsAsync<BusinessException>(() => service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = instrument.Id }));
    }

    [Fact]
    public async Task CreateRequestAsync_Succeeds_WhenInstrumentAlreadyLocked()
    {

        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await SeedInstrumentAsync(context);
        var existingRental = new InstrumentRental(instrument.Id, 999, instrument.MusicStoreId, Now, null);
        existingRental.Approve(50m, null, Now, 1);
        context.Set<InstrumentRental>().Add(existingRental);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var result = await service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = instrument.Id });

        Assert.NotNull(result);
        Assert.Equal(InstrumentRentalStatus.Pending, result.RentalStatus);
    }

    [Fact]
    public async Task CreateRequestAsync_CreatesRental_WhenValid()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await SeedInstrumentAsync(context);
        var service = CreateService(context, student);

        var request = new RentalCreateRequest { InstrumentId = instrument.Id, Note = "please" };
        var result = await service.CreateRequestAsync(request);

        Assert.NotNull(result);
        Assert.Equal(instrument.Id, result.InstrumentId);
        Assert.Equal(InstrumentRentalStatus.Pending, result.RentalStatus);

        var rental = await context.Set<InstrumentRental>().SingleAsync(x => x.InstrumentId == instrument.Id && x.StudentProfileId == student.Id);
        Assert.NotNull(rental);
    }

    private static ENoteContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ENoteContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ENoteContext(options, new FixedClock(Now), new StubCurrentActor(new Student(0, Now))) { ExplicitStoreId = 1 };
    }

    private static async Task<Student> SeedStudentAsync(ENoteContext context, bool hasActiveMembership)
    {
        var student = new Student(appUserId: 100, enrollmentDate: Now.AddMonths(-1));
        student.UpdateMembership(hasActiveMembership ? Now.AddDays(1) : Now.AddDays(-1));
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

    [Fact]
    public async Task CreateRequestAsync_DispatchesCreatedNotification()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await SeedInstrumentAsync(context);
        var recorder = new RecordingNotificationDispatcher();
        var service = CreateService(context, student, recorder);

        await service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = instrument.Id });

        Assert.Single(recorder.CreatedCalls);
    }

    [Fact]
    public async Task ApproveAsync_DispatchesTransitionNotification()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var recorder = new RecordingNotificationDispatcher();
        var service = CreateStoreService(context, instrument.MusicStoreId, recorder);

        await service.ApproveAsync(rental.Id, new RentalStatusResponse());

        Assert.Single(recorder.TransitionCalls);
        Assert.Equal(RentalTrigger.Approve, recorder.TransitionCalls[0].Trigger);
    }

    private static RentalCommandService CreateService(ENoteContext context, Student student, IRentalNotificationDispatcher? dispatcher = null)
    {
        var currentUser = new StubCurrentActor(student: student);
        return new(context, TestMapper.Create(), new FixedClock(Now), currentUser, currentUser, currentUser,
            dispatcher ?? new NoOpNotificationDispatcher());
    }

    private static RentalCommandService CreateStoreService(ENoteContext context, int storeId, IRentalNotificationDispatcher? dispatcher = null)
    {
        var currentUser = new StubCurrentActor(storeId: storeId);
        return new(context, TestMapper.Create(), new FixedClock(Now), currentUser, currentUser, currentUser,
            dispatcher ?? new NoOpNotificationDispatcher());
    }
}
