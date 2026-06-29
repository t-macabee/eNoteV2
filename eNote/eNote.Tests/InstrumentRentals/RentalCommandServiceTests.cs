using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;
using eNote.Domain.Entities;
using eNote.Domain.Entities.Rentals;
using eNote.Domain.Enums;
using eNote.Infrastructure.Data;
using eNote.Tests.TestUtils;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace eNote.Tests.InstrumentRentals;

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
        // Pre-check removed: Pending requests are accepted even for reserved instruments.
        // The unique index enforces exclusivity at Approve/Pickup time, not at request creation.
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
        return new ENoteContext(options, new FixedClock(Now), new StubCurrentActor(new Student(0, Now)));
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

    private static RentalCommandService CreateService(ENoteContext context, Student student)
    {
        var mapper = new Mapper();
        return new RentalCommandService(
            context,
            mapper,
            new FixedClock(Now),
            new StubCurrentActor(student: student),
            new RentalStateMachine(new FixedClock(Now)),
            new NoOpNotificationDispatcher());
    }
}
