using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users.Services;
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
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var service = CreateService(context, student);

        await Assert.ThrowsAsync<BusinessException>(() => service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = instrument.Id }));
    }

    [Fact]
    public async Task CreateRequestAsync_Succeeds_WhenInstrumentAlreadyLocked()
    {

        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
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
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var service = CreateService(context, student);

        var request = new RentalCreateRequest { InstrumentId = instrument.Id, Note = "please" };
        var result = await service.CreateRequestAsync(request);

        Assert.NotNull(result);
        Assert.Equal(instrument.Id, result.InstrumentId);
        Assert.Equal(InstrumentRentalStatus.Pending, result.RentalStatus);

        var rental = await context.Set<InstrumentRental>().SingleAsync(x => x.InstrumentId == instrument.Id && x.StudentProfileId == student.Id);
        Assert.NotNull(rental);
    }

    [Fact]
    public async Task CreateRequestAsync_BlockedByUnpaidDebt()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var unpaid = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-20), null);
        unpaid.Approve(50m, null, Now.AddDays(-19), 1);
        unpaid.Pickup(Now.AddDays(-19));
        unpaid.Complete(Now.AddDays(-5), null);
        context.Set<InstrumentRental>().Add(unpaid);
        await context.SaveChangesAsync();

        var newInstrument = await SeedExtraInstrumentAsync(context, instrument);
        var service = CreateService(context, student);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = newInstrument.Id }));

        Assert.Equal(Messages.RentalUnpaidDebt, ex.Message);
    }

    [Fact]
    public async Task CreateRequestAsync_BlockedByUnpaidReturnedEarlyDebt()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var unpaid = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-20), null);
        unpaid.Approve(50m, null, Now.AddDays(-19), 1);
        unpaid.Pickup(Now.AddDays(-19));
        unpaid.ReturnEarly(Now.AddDays(-5), null);
        context.Set<InstrumentRental>().Add(unpaid);
        await context.SaveChangesAsync();

        var newInstrument = await SeedExtraInstrumentAsync(context, instrument);
        var service = CreateService(context, student);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = newInstrument.Id }));

        Assert.Equal(Messages.RentalUnpaidDebt, ex.Message);
    }

    [Fact]
    public async Task CreateRequestAsync_BlockedByUnpaidDebt_WhenInstrumentSoftDeleted()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var unpaid = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-20), null);
        unpaid.Approve(50m, null, Now.AddDays(-19), 1);
        unpaid.Pickup(Now.AddDays(-19));
        unpaid.Complete(Now.AddDays(-5), null);
        context.Set<InstrumentRental>().Add(unpaid);
        await context.SaveChangesAsync();

        instrument.SoftDelete();
        await context.SaveChangesAsync();

        var newInstrument = await SeedExtraInstrumentAsync(context, instrument);
        var service = CreateService(context, student);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = newInstrument.Id }));

        Assert.Equal(Messages.RentalUnpaidDebt, ex.Message);
    }

    [Fact]
    public async Task CreateRequestAsync_AllowedWhenAllPastRentalsPaid()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var paid = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-20), null);
        paid.Approve(50m, null, Now.AddDays(-19), 1);
        paid.Pickup(Now.AddDays(-19));
        paid.Complete(Now.AddDays(-5), null);
        paid.MarkPaid(5000, Now);
        context.Set<InstrumentRental>().Add(paid);
        await context.SaveChangesAsync();

        var newInstrument = await SeedExtraInstrumentAsync(context, instrument);
        var service = CreateService(context, student);

        var result = await service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = newInstrument.Id });

        Assert.NotNull(result);
        Assert.Equal(InstrumentRentalStatus.Pending, result.RentalStatus);
    }

    [Fact]
    public async Task CreateRequestAsync_Allowed_WhenUnpaidRentalIsNotTerminal()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);

        var pending = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(pending);

        var approvedInstrument = await SeedExtraInstrumentAsync(context, instrument);
        var approved = new InstrumentRental(approvedInstrument.Id, student.Id, approvedInstrument.MusicStoreId, Now.AddDays(-5), null);
        approved.Approve(50m, null, Now.AddDays(-4), 1);
        context.Set<InstrumentRental>().Add(approved);

        var activeInstrument = await SeedExtraInstrumentAsync(context, instrument);
        var active = new InstrumentRental(activeInstrument.Id, student.Id, activeInstrument.MusicStoreId, Now.AddDays(-5), null);
        active.Approve(50m, null, Now.AddDays(-4), 1);
        active.Pickup(Now.AddDays(-4));
        context.Set<InstrumentRental>().Add(active);

        await context.SaveChangesAsync();

        var newInstrument = await SeedExtraInstrumentAsync(context, instrument);
        var service = CreateService(context, student);

        var result = await service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = newInstrument.Id });

        Assert.NotNull(result);
        Assert.Equal(InstrumentRentalStatus.Pending, result.RentalStatus);
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

    private static async Task<Instrument> SeedExtraInstrumentAsync(ENoteContext context, Instrument reference)
    {
        var instrument = new Instrument("Second Model", "Second Manufacturer", null, null, reference.InstrumentTypeId, reference.MusicStoreId);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();
        return instrument;
    }

    [Fact]
    public async Task CreateRequestAsync_DispatchesCreatedNotification()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
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
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var recorder = new RecordingNotificationDispatcher();
        var service = CreateStoreService(context, instrument.MusicStoreId, recorder);

        await service.ApproveAsync(rental.Id, new RentalStatusRequest());

        Assert.Single(recorder.TransitionCalls);
        Assert.Equal(RentalTrigger.Approve, recorder.TransitionCalls[0].Trigger);
    }

    [Fact]
    public async Task CancelForStoreAsync_Succeeds_FromApproved()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        rental.Approve(50m, null, Now, 1);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateStoreService(context, instrument.MusicStoreId);

        var result = await service.CancelForStoreAsync(rental.Id, new RentalStatusRequest { Note = "No show" });

        Assert.NotNull(result);
        Assert.Equal(InstrumentRentalStatus.Canceled, result.RentalStatus);
        Assert.Equal("No show", result.Note);
    }

    [Fact]
    public async Task CancelForStoreAsync_Throws_WhenAlreadyPickedUp()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        rental.Approve(50m, null, Now, 1);
        rental.Pickup(Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateStoreService(context, instrument.MusicStoreId);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CancelForStoreAsync(rental.Id, new RentalStatusRequest()));
        Assert.Equal(Messages.RentalCancelPendingOrApprovedOnly, ex.Message);
    }

    [Fact]
    public async Task CancelForStoreAsync_DispatchesTransitionNotification()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        rental.Approve(50m, null, Now, 1);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var recorder = new RecordingNotificationDispatcher();
        var service = CreateStoreService(context, instrument.MusicStoreId, recorder);

        await service.CancelForStoreAsync(rental.Id, new RentalStatusRequest { Note = "Cancelled by store" });

        Assert.Single(recorder.TransitionCalls);
        Assert.Equal(RentalTrigger.Cancel, recorder.TransitionCalls[0].Trigger);
    }

    [Fact]
    public async Task CreateRequestAsync_Throws_WhenRentalAlreadyPending()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var pendingRental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(pendingRental);
        await context.SaveChangesAsync();

        var service = CreateService(context, student);
        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = instrument.Id }));
        Assert.Equal(Messages.RentalPendingRequired, ex.Message);
    }

    [Fact]
    public async Task CreateRequestAsync_TreatsPendingUniqueIndexViolation_AsBusinessException()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.InstrumentRentalPendingUniqueIndex}\"");
        var throwingContext = new ThrowingSaveDbContext(context, new DbUpdateException("Unique constraint violated.", inner));
        var service = CreateService(throwingContext, student);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = instrument.Id }));
        Assert.Equal(Messages.RentalPendingRequired, ex.Message);
    }

    [Fact]
    public async Task ApproveAsync_TreatsActiveOrApprovedUniqueIndexViolation_AsBusinessException()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var pendingRental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(pendingRental);
        await context.SaveChangesAsync();

        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.InstrumentRentalActiveOrApprovedUniqueIndex}\"");
        var throwingContext = new ThrowingSaveDbContext(context, new DbUpdateException("Unique constraint violated.", inner));
        var service = CreateStoreService(throwingContext, instrument.MusicStoreId);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.ApproveAsync(pendingRental.Id, null));
        Assert.Equal(Messages.InstrumentReservedOrRented, ex.Message);
    }

    [Fact]
    public async Task ApproveAsync_TreatsConcurrencyViolation_AsConflict()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var pendingRental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(pendingRental);
        await context.SaveChangesAsync();

        var throwingContext = new ThrowingSaveDbContext(context, new DbUpdateConcurrencyException("Concurrency conflict."));
        var service = CreateStoreService(throwingContext, instrument.MusicStoreId);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.ApproveAsync(pendingRental.Id, null));
        Assert.Equal(Messages.ConcurrencyConflict, ex.Message);
    }

    [Fact]
    public async Task CreateRequestAsync_PropagatesUnrelatedDbUpdateException()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var inner = new Exception("unrelated constraint");
        var throwingContext = new ThrowingSaveDbContext(context, new DbUpdateException("DB error.", inner));
        var service = CreateService(throwingContext, student);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = instrument.Id }));
    }

    private static RentalCommandService CreateService(IAppDbContext context, Student student, IRentalNotificationDispatcher? dispatcher = null)
    {
        var currentUser = new StubCurrentActor(student: student);
        return new(context, TestMapper.Create(), new FixedClock(Now), currentUser, currentUser, currentUser,
            dispatcher ?? new NoOpNotificationDispatcher(), new StubDisplayNameService());
    }

    private static RentalCommandService CreateStoreService(IAppDbContext context, int storeId, IRentalNotificationDispatcher? dispatcher = null)
    {
        var currentUser = new StubCurrentActor(storeId: storeId);
        return new(context, TestMapper.Create(), new FixedClock(Now), currentUser, currentUser, currentUser,
            dispatcher ?? new NoOpNotificationDispatcher(), new StubDisplayNameService());
    }
}
