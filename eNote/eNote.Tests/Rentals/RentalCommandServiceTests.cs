using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Tests.TestUtils;
using eNote.Worker;
using Microsoft.EntityFrameworkCore;

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateRequestAsync_Throws_WhenInstrumentAlreadyReservedOrRented(bool pickedUp)
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var existingRental = new InstrumentRental(instrument.Id, 999, instrument.MusicStoreId, Now, null);
        RentalTestData.AdvanceTo(
            existingRental,
            pickedUp ? InstrumentRentalStatus.Active : InstrumentRentalStatus.Approved,
            Now);

        context.Set<InstrumentRental>().Add(existingRental);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = instrument.Id }));

        Assert.Equal(Messages.InstrumentReservedOrRented, ex.Message);
        Assert.False(await context.Set<InstrumentRental>().AnyAsync(x => x.StudentProfileId == student.Id));
    }

    [Fact]
    public async Task CreateRequestAsync_Throws_WhenInstrumentArchived()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        instrument.SoftDelete();
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = instrument.Id }));

        Assert.Equal(Messages.InstrumentNotFound, ex.Message);
        Assert.False(await context.Set<InstrumentRental>().AnyAsync(x => x.StudentProfileId == student.Id));
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
    public async Task CreateRequestAsync_ReportsHowManyOtherRequestsArePending()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        context.Set<InstrumentRental>().AddRange(
            new InstrumentRental(instrument.Id, 998, instrument.MusicStoreId, Now, null),
            new InstrumentRental(instrument.Id, 999, instrument.MusicStoreId, Now, null));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var result = await service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = instrument.Id });

        Assert.Equal(2, result.OtherPendingRequests);
    }

    [Fact]
    public async Task CreateRequestAsync_CountsOnlyPendingRequestsForSameInstrument()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var otherInstrument = await SeedExtraInstrumentAsync(context, instrument);
        var canceled = new InstrumentRental(instrument.Id, 997, instrument.MusicStoreId, Now, null);
        RentalTestData.AdvanceTo(canceled, InstrumentRentalStatus.Canceled, Now, note: "Nije mi više potrebno.");
        context.Set<InstrumentRental>().AddRange(
            new InstrumentRental(instrument.Id, 998, instrument.MusicStoreId, Now, null),
            new InstrumentRental(otherInstrument.Id, 999, otherInstrument.MusicStoreId, Now, null),
            canceled);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var result = await service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = instrument.Id });

        Assert.Equal(1, result.OtherPendingRequests);
    }

    [Fact]
    public async Task CreateRequestAsync_ReportsZeroOtherPendingRequests_WhenNobodyElseWaits()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var service = CreateService(context, student);

        var result = await service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = instrument.Id });

        Assert.Equal(0, result.OtherPendingRequests);
    }

    [Fact]
    public async Task CreateRequestAsync_BlockedByUnpaidDebt()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var unpaid = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-20), null);
        RentalTestData.AdvanceTo(unpaid, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-35), pickedUpAt: Now.AddDays(-35), finishedAt: Now.AddDays(-5));
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
        RentalTestData.AdvanceTo(unpaid, InstrumentRentalStatus.ReturnedEarly, Now, approvedAt: Now.AddDays(-19), pickedUpAt: Now.AddDays(-19), finishedAt: Now.AddDays(-5));
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
        RentalTestData.AdvanceTo(unpaid, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-35), pickedUpAt: Now.AddDays(-35), finishedAt: Now.AddDays(-5));
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
        RentalTestData.AdvanceTo(paid, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-35), pickedUpAt: Now.AddDays(-35), finishedAt: Now.AddDays(-5));
        paid.MarkPaid(5000, Now);
        context.Set<InstrumentRental>().Add(paid);
        await context.SaveChangesAsync();

        var newInstrument = await SeedExtraInstrumentAsync(context, instrument);
        var service = CreateService(context, student);

        var result = await service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = newInstrument.Id });

        Assert.NotNull(result);
        Assert.Equal(InstrumentRentalStatus.Pending, result.RentalStatus);
    }

    [Theory]
    [InlineData(InstrumentRentalStatus.Canceled)]
    [InlineData(InstrumentRentalStatus.Rejected)]
    [InlineData(InstrumentRentalStatus.Completed)]
    public async Task CreateRequestAsync_Allowed_AfterTerminalRentalOfSameInstrument(InstrumentRentalStatus terminalStatus)
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var previous = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-20), null);

        switch (terminalStatus)
        {
            case InstrumentRentalStatus.Canceled:
                RentalTestData.AdvanceTo(previous, InstrumentRentalStatus.Canceled, Now, note: "Nije mi više potrebno.");
                break;
            case InstrumentRentalStatus.Rejected:
                RentalTestData.AdvanceTo(previous, InstrumentRentalStatus.Rejected, Now.AddDays(-19), note: "Instrument je na servisu.");
                break;
            case InstrumentRentalStatus.Completed:
                RentalTestData.AdvanceTo(previous, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-35), pickedUpAt: Now.AddDays(-35), finishedAt: Now.AddDays(-5));
                previous.MarkPaid(5000, Now);
                break;
        }

        context.Set<InstrumentRental>().Add(previous);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var result = await service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = instrument.Id });

        Assert.Equal(InstrumentRentalStatus.Pending, result.RentalStatus);
    }

    [Fact]
    public async Task CreateRequestAsync_Allowed_WhenAnotherStudentHasUnpaidDebt()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var stranger = new Student(appUserId: 101, enrollmentDate: Now.AddMonths(-1));
        context.Set<Student>().Add(stranger);
        await context.SaveChangesAsync();
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var unpaid = new InstrumentRental(instrument.Id, stranger.Id, instrument.MusicStoreId, Now.AddDays(-20), null);
        RentalTestData.AdvanceTo(unpaid, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-35), pickedUpAt: Now.AddDays(-35), finishedAt: Now.AddDays(-5));
        context.Set<InstrumentRental>().Add(unpaid);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var result = await service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = instrument.Id });

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
        RentalTestData.AdvanceTo(approved, InstrumentRentalStatus.Approved, Now, approvedAt: Now.AddDays(-4));
        context.Set<InstrumentRental>().Add(approved);

        var activeInstrument = await SeedExtraInstrumentAsync(context, instrument);
        var active = new InstrumentRental(activeInstrument.Id, student.Id, activeInstrument.MusicStoreId, Now.AddDays(-5), null);
        RentalTestData.AdvanceTo(active, InstrumentRentalStatus.Active, Now, approvedAt: Now.AddDays(-4), pickedUpAt: Now.AddDays(-4));
        context.Set<InstrumentRental>().Add(active);

        await context.SaveChangesAsync();

        var newInstrument = await SeedExtraInstrumentAsync(context, instrument);
        var service = CreateService(context, student);

        var result = await service.CreateRequestAsync(new RentalCreateRequest { InstrumentId = newInstrument.Id });

        Assert.NotNull(result);
        Assert.Equal(InstrumentRentalStatus.Pending, result.RentalStatus);
    }

    [Fact]
    public async Task ExpireUnclaimedApprovalsAsync_RejectsOnlyApprovalsPastTheDeadline()
    {
        var dbName = Guid.NewGuid().ToString();
        int dueId;
        int notDueId;
        int activeId;
        int pendingId;

        await using (var context = CreateWorkerContext(dbName))
        {
            var student = new Student(appUserId: 100, enrollmentDate: Now.AddMonths(-1));
            context.Set<Student>().Add(student);
            await context.SaveChangesAsync();

            dueId = await SeedRentalAsync(context, student, status: InstrumentRentalStatus.Approved, approvedAt: Now.AddDays(-2));
            notDueId = await SeedRentalAsync(context, student, status: InstrumentRentalStatus.Approved, approvedAt: Now.AddDays(-2).AddMinutes(1));
            activeId = await SeedRentalAsync(context, student, status: InstrumentRentalStatus.Active, approvedAt: Now.AddDays(-5), pickedUpAt: Now.AddDays(-4));
            pendingId = await SeedRentalAsync(context, student, status: InstrumentRentalStatus.Pending);
        }

        var recorder = new RecordingNotificationDispatcher();

        await using (var context = CreateWorkerContext(dbName))
        {
            var service = CreateWorkerService(context, recorder);

            var count = await service.ExpireUnclaimedApprovalsAsync();

            Assert.Equal(1, count);
        }

        await using (var context = CreateWorkerContext(dbName))
        {
            var due = await context.Set<InstrumentRental>().AsNoTracking().SingleAsync(x => x.Id == dueId);
            Assert.Equal(InstrumentRentalStatus.Rejected, due.RentalStatus);
            Assert.Equal(Now, due.RejectedAt);
            Assert.Null(due.RejectedById);
            Assert.Equal(Messages.RentalRejectedPickupExpired, due.Note);

            var notDue = await context.Set<InstrumentRental>().AsNoTracking().SingleAsync(x => x.Id == notDueId);
            Assert.Equal(InstrumentRentalStatus.Approved, notDue.RentalStatus);

            var active = await context.Set<InstrumentRental>().AsNoTracking().SingleAsync(x => x.Id == activeId);
            Assert.Equal(InstrumentRentalStatus.Active, active.RentalStatus);

            var pending = await context.Set<InstrumentRental>().AsNoTracking().SingleAsync(x => x.Id == pendingId);
            Assert.Equal(InstrumentRentalStatus.Pending, pending.RentalStatus);
        }

        var transitionCall = Assert.Single(recorder.TransitionCalls);
        Assert.Equal(dueId, transitionCall.Rental.Id);
        Assert.Equal(RentalTrigger.ExpirePickup, transitionCall.Trigger);
        Assert.Null(transitionCall.ActorUserId);
    }

    [Fact]
    public async Task ExpireUnclaimedApprovalsAsync_ReturnsZero_WhenNothingIsDue()
    {
        var dbName = Guid.NewGuid().ToString();
        int rentalId;

        await using (var context = CreateWorkerContext(dbName))
        {
            var student = new Student(appUserId: 100, enrollmentDate: Now.AddMonths(-1));
            context.Set<Student>().Add(student);
            await context.SaveChangesAsync();

            rentalId = await SeedRentalAsync(context, student, status: InstrumentRentalStatus.Approved, approvedAt: Now.AddHours(-47));
        }

        var recorder = new RecordingNotificationDispatcher();

        await using (var context = CreateWorkerContext(dbName))
        {
            var service = CreateWorkerService(context, recorder);

            var count = await service.ExpireUnclaimedApprovalsAsync();

            Assert.Equal(0, count);
        }

        await using (var context = CreateWorkerContext(dbName))
        {
            var stored = await context.Set<InstrumentRental>().AsNoTracking().SingleAsync(x => x.Id == rentalId);
            Assert.Equal(InstrumentRentalStatus.Approved, stored.RentalStatus);
        }

        Assert.Empty(recorder.TransitionCalls);
    }

    [Fact]
    public async Task RenewAsync_ByOwner_AddsOneTerm_AndDispatchesRenew()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-28), null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Active, Now, approvedAt: Now.AddDays(-28), pickedUpAt: Now.AddDays(-27));
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var recorder = new RecordingNotificationDispatcher();
        var service = CreateService(context, student, recorder);

        var dueBefore = rental.DueAt;
        var result = await service.RenewAsync(rental.Id);

        Assert.Equal(dueBefore!.Value.AddDays(30), result.DueAt);
        var stored = await context.Set<InstrumentRental>().AsNoTracking().SingleAsync(x => x.Id == rental.Id);
        Assert.Equal(dueBefore.Value.AddDays(30), stored.DueAt);
        var transition = Assert.Single(recorder.TransitionCalls);
        Assert.Equal(RentalTrigger.Renew, transition.Trigger);
    }

    [Fact]
    public async Task RenewAsync_ByAnotherStudent_ThrowsNotFound()
    {
        await using var context = CreateContext();
        var owner = await SeedStudentAsync(context, hasActiveMembership: true);
        var stranger = new Student(appUserId: 101, enrollmentDate: Now.AddMonths(-1));
        context.Set<Student>().Add(stranger);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, owner.Id, instrument.MusicStoreId, Now.AddDays(-28), null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Active, Now, approvedAt: Now.AddDays(-28), pickedUpAt: Now.AddDays(-27));
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, stranger);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.RenewAsync(rental.Id));

        Assert.Equal(Messages.RentalNotFound, ex.Message);
    }

    [Fact]
    public async Task RenewAsync_BeforeWindow_ThrowsBusinessException_AndLeavesDueAt()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-11), null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Active, Now, approvedAt: Now.AddDays(-11), pickedUpAt: Now.AddDays(-10));
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var dueBefore = rental.DueAt;
        var service = CreateService(context, student);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.RenewAsync(rental.Id));

        Assert.Equal("Produženje je moguće samo u zadnjih 5 dana trenutnog roka.", ex.Message);
        var stored = await context.Set<InstrumentRental>().AsNoTracking().SingleAsync(x => x.Id == rental.Id);
        Assert.Equal(dueBefore, stored.DueAt);
    }

    [Fact]
    public async Task ApproveAsync_CopiesTheConfiguredPenaltyMultiplier()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateStoreService(context, instrument.MusicStoreId, rentalOptions: new RentalOptions { OverduePenaltyMultiplier = 0.75m });

        await service.ApproveAsync(rental.Id, new RentalStatusRequest());

        var stored = await context.Set<InstrumentRental>().AsNoTracking().SingleAsync(x => x.Id == rental.Id);
        Assert.Equal(0.75m, stored.OverduePenaltyMultiplier);
    }

    private static ENoteContext CreateWorkerContext(string dbName) =>
        new(TestDbContextFactory.CreateOptions(dbName), new FixedClock(Now), new WorkerActor());

    private static RentalCommandService CreateWorkerService(IAppDbContext context, IRentalNotificationDispatcher dispatcher)
    {
        var actor = new WorkerActor();
        var students = new StubCurrentActor(new Student(0, Now));
        return new(context, TestMapper.Create(), new FixedClock(Now), actor, students, students, dispatcher, new StubDisplayNameService(), new RentalOptions { OverduePenaltyMultiplier = 0.5m });
    }

    private static async Task<int> SeedRentalAsync(
        ENoteContext context,
        Student student,
        InstrumentRentalStatus status,
        DateTime? approvedAt = null,
        DateTime? pickedUpAt = null)
    {
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-5), null);
        RentalTestData.AdvanceTo(rental, status, Now, approvedAt: approvedAt, pickedUpAt: pickedUpAt);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        return rental.Id;
    }

    private static ENoteContext CreateContext() =>
        new(TestDbContextFactory.CreateOptions(), new FixedClock(Now), new StubCurrentActor(new Student(0, Now))) { ExplicitStoreId = 1 };

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
    public async Task ApproveAsync_Throws_WhenInstrumentArchived()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        instrument.SoftDelete();
        await context.SaveChangesAsync();
        var service = CreateStoreService(context, instrument.MusicStoreId);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.ApproveAsync(rental.Id, new RentalStatusRequest()));

        Assert.Equal("Instrument više nije aktivan pa zahtjev ne može biti odobren.", ex.Message);
        var stored = await context.Set<InstrumentRental>().AsNoTracking().SingleAsync(x => x.Id == rental.Id);
        Assert.Equal(InstrumentRentalStatus.Pending, stored.RentalStatus);
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
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Approved, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateStoreService(context, instrument.MusicStoreId);

        var result = await service.CancelForStoreAsync(rental.Id, new RentalStatusRequest { Note = "No show" });

        Assert.NotNull(result);
        Assert.Equal(InstrumentRentalStatus.Canceled, result.RentalStatus);
        Assert.Equal("No show", result.Note);
    }

    [Fact]
    public async Task CancelAsync_Throws_WhenRentalBelongsToAnotherStudent()
    {
        await using var context = CreateContext();
        var owner = await SeedStudentAsync(context, hasActiveMembership: true);
        var stranger = new Student(appUserId: 101, enrollmentDate: Now.AddMonths(-1));
        context.Set<Student>().Add(stranger);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, owner.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateService(context, stranger);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.CancelAsync(rental.Id, new RentalStatusRequest { Note = "Nije mi više potrebno." }));

        Assert.Equal(Messages.RentalNotFound, ex.Message);
        var stored = await context.Set<InstrumentRental>().AsNoTracking().SingleAsync(x => x.Id == rental.Id);
        Assert.Equal(InstrumentRentalStatus.Pending, stored.RentalStatus);
    }

    [Fact]
    public async Task ApproveAsync_Throws_WhenInstrumentIsAlreadyApprovedForAnotherStudent()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var first = new InstrumentRental(instrument.Id, 998, instrument.MusicStoreId, Now, null);
        RentalTestData.AdvanceTo(first, InstrumentRentalStatus.Approved, Now);
        var second = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().AddRange(first, second);
        await context.SaveChangesAsync();
        var service = CreateStoreService(context, instrument.MusicStoreId);

        await Assert.ThrowsAsync<BusinessException>(() => service.ApproveAsync(second.Id, new RentalStatusRequest()));

        var stored = await context.Set<InstrumentRental>().AsNoTracking().SingleAsync(x => x.Id == second.Id);
        Assert.Equal(InstrumentRentalStatus.Pending, stored.RentalStatus);
    }

    [Fact]
    public async Task ApproveAsync_Succeeds_WhenAnotherInstrumentIsActive()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var otherInstrument = await SeedExtraInstrumentAsync(context, instrument);
        var active = new InstrumentRental(otherInstrument.Id, 998, otherInstrument.MusicStoreId, Now.AddDays(-5), null);
        RentalTestData.AdvanceTo(active, InstrumentRentalStatus.Active, Now, approvedAt: Now.AddDays(-4), pickedUpAt: Now.AddDays(-4));
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().AddRange(active, rental);
        await context.SaveChangesAsync();
        var service = CreateStoreService(context, instrument.MusicStoreId);

        var result = await service.ApproveAsync(rental.Id, new RentalStatusRequest());

        Assert.Equal(InstrumentRentalStatus.Approved, result.RentalStatus);
    }

    [Fact]
    public async Task CancelForStoreAsync_Throws_WhenAlreadyPickedUp()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Active, Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();
        var service = CreateStoreService(context, instrument.MusicStoreId);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CancelForStoreAsync(rental.Id, new RentalStatusRequest { Note = "Instrument je vraćen." }));
        Assert.Equal(Messages.RentalCancelPendingOrApprovedOnly, ex.Message);
    }

    [Fact]
    public async Task CancelForStoreAsync_DispatchesTransitionNotification()
    {
        await using var context = CreateContext();
        var student = await SeedStudentAsync(context, hasActiveMembership: true);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Approved, Now);
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

    private static RentalCommandService CreateService(IAppDbContext context, Student student, IRentalNotificationDispatcher? dispatcher = null, RentalOptions? rentalOptions = null)
    {
        var currentUser = new StubCurrentActor(student: student);
        return new(context, TestMapper.Create(), new FixedClock(Now), currentUser, currentUser, currentUser,
            dispatcher ?? new RecordingNotificationDispatcher(), new StubDisplayNameService(), rentalOptions ?? new RentalOptions { OverduePenaltyMultiplier = 0.5m });
    }

    private static RentalCommandService CreateStoreService(IAppDbContext context, int storeId, IRentalNotificationDispatcher? dispatcher = null, RentalOptions? rentalOptions = null)
    {
        var currentUser = new StubCurrentActor(storeId: storeId);
        return new(context, TestMapper.Create(), new FixedClock(Now), currentUser, currentUser, currentUser,
            dispatcher ?? new RecordingNotificationDispatcher(), new StubDisplayNameService(), rentalOptions ?? new RentalOptions { OverduePenaltyMultiplier = 0.5m });
    }
}
