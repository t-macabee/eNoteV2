using eNote.Tests.TestUtils;

namespace eNote.Tests.Rentals;

public sealed class InstrumentRentalTransitionTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Reject_FromPending_SetsRejectedStatus()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, "note");
        var context = new RentalTransitionContext
        {
            UserId = 10,
            Actor = RentalActor.StoreEmployee,
            HasInstrumentLockConflict = false,
            IsInstrumentInactive = false,
            MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m,
            ResponseNote = "Instrument je na servisu."
        };

        var result = rental.Transition(RentalTrigger.Reject, context, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.Rejected, rental.RentalStatus);
        Assert.NotNull(rental.RejectedAt);
        Assert.Equal(10, rental.RejectedById);
        Assert.Equal("Instrument je na servisu.", rental.Note);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WithoutNote_Fails(string? note)
    {
        var rental = new InstrumentRental(1, 1, 1, Now, "note");
        var context = new RentalTransitionContext
        {
            UserId = 10,
            Actor = RentalActor.StoreEmployee,
            HasInstrumentLockConflict = false,
            IsInstrumentInactive = false,
            MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m,
            ResponseNote = note
        };

        var result = rental.Transition(RentalTrigger.Reject, context, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal("Razlog odbijanja je obavezan.", result.Error);
        Assert.Equal(InstrumentRentalStatus.Pending, rental.RentalStatus);
        Assert.Null(rental.RejectedAt);
    }

    [Fact]
    public void Cancel_FromPending_WithoutPickup_SetsCanceledStatus()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        var context = new RentalTransitionContext
        {
            UserId = 5,
            Actor = RentalActor.Student,
            HasInstrumentLockConflict = false,
            IsInstrumentInactive = false,
            MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m,
            ResponseNote = "Nije mi više potrebno."
        };

        var result = rental.Transition(RentalTrigger.Cancel, context, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.Canceled, rental.RentalStatus);
        Assert.Equal("Nije mi više potrebno.", rental.Note);
        Assert.Null(rental.ReturnedAt);
    }

    [Theory]
    [InlineData(InstrumentRentalStatus.Pending, RentalActor.Student, null)]
    [InlineData(InstrumentRentalStatus.Approved, RentalActor.Student, null)]
    [InlineData(InstrumentRentalStatus.Approved, RentalActor.StoreEmployee, null)]
    [InlineData(InstrumentRentalStatus.Pending, RentalActor.Student, "   ")]
    [InlineData(InstrumentRentalStatus.Approved, RentalActor.Student, "   ")]
    [InlineData(InstrumentRentalStatus.Approved, RentalActor.StoreEmployee, "   ")]
    public void Cancel_WithoutNote_Fails(InstrumentRentalStatus from, RentalActor actor, string? note)
    {
        var rental = BuildRentalIn(from);
        var context = new RentalTransitionContext
        {
            UserId = 10,
            Actor = actor,
            HasInstrumentLockConflict = false,
            IsInstrumentInactive = false,
            MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m,
            ResponseNote = note
        };

        var result = rental.Transition(RentalTrigger.Cancel, context, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal("Razlog otkazivanja je obavezan.", result.Error);
        Assert.Equal(from, rental.RentalStatus);
    }

    [Fact]
    public void Fire_WithInvalidTransition_ReturnsFailure()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        var context = new RentalTransitionContext
        {
            UserId = 10,
            Actor = RentalActor.StoreEmployee,
            HasInstrumentLockConflict = false,
            IsInstrumentInactive = false,
            MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m
        };

        var result = rental.Transition(RentalTrigger.Pickup, context, Now);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Complete_FromPending_ReturnsInvalidTransition()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m };

        var result = rental.Transition(RentalTrigger.Complete, ctx, Now);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Approve_Fails_WhenLockConflict()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = true, IsInstrumentInactive = false, MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m };

        var result = rental.Transition(RentalTrigger.Approve, ctx, Now);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Approve_Succeeds_SetsApprovedStatus()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m };

        var result = rental.Transition(RentalTrigger.Approve, ctx, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.Approved, rental.RentalStatus);
        Assert.Equal(50m, rental.Fee);
        Assert.Equal(Now, rental.ApprovedAt);
        Assert.Equal(10, rental.ApprovedById);
    }

    [Fact]
    public void Pickup_Succeeds_SetsActiveStatus()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Approved, Now, userId: 10);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m };

        var result = rental.Transition(RentalTrigger.Pickup, ctx, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.Active, rental.RentalStatus);
        Assert.NotNull(rental.PickedUpAt);
    }

    [Fact]
    public void Complete_Succeeds_SetsCompletedStatus()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Active, Now, userId: 10);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m };

        var result = rental.Transition(RentalTrigger.Complete, ctx, Now.AddDays(30));

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.Completed, rental.RentalStatus);
        Assert.NotNull(rental.ReturnedAt);
    }

    [Fact]
    public void ReturnEarly_Succeeds_SetsReturnedEarlyStatus()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Active, Now, userId: 10);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m };

        var result = rental.Transition(RentalTrigger.ReturnEarly, ctx, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.ReturnedEarly, rental.RentalStatus);
    }

    [Fact]
    public void ExpirePickup_OneSecondBeforeDeadline_Fails()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Approved, Now, userId: 10, approvedAt: Now.AddDays(-2).AddSeconds(1));
        var ctx = new RentalTransitionContext { UserId = null, Actor = RentalActor.System, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m, ResponseNote = "Razlog." };

        var result = rental.Transition(RentalTrigger.ExpirePickup, ctx, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal("Rok za preuzimanje instrumenta još nije istekao.", result.Error);
        Assert.Equal(InstrumentRentalStatus.Approved, rental.RentalStatus);
    }

    [Fact]
    public void ExpirePickup_AtDeadline_RejectsWithoutUser()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Approved, Now, userId: 10, approvedAt: Now.AddDays(-2));
        var ctx = new RentalTransitionContext { UserId = null, Actor = RentalActor.System, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m, ResponseNote = "Nije preuzet." };

        var result = rental.Transition(RentalTrigger.ExpirePickup, ctx, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.Rejected, rental.RentalStatus);
        Assert.Equal(Now, rental.RejectedAt);
        Assert.Null(rental.RejectedById);
        Assert.Equal("Nije preuzet.", rental.Note);
        Assert.Equal(10, rental.ApprovedById);
    }

    [Fact]
    public void ExpirePickup_WithoutReason_Fails()
    {
        var rental = BuildRentalIn(InstrumentRentalStatus.Approved);
        var ctx = new RentalTransitionContext { UserId = null, Actor = RentalActor.System, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m };

        var result = rental.Transition(RentalTrigger.ExpirePickup, ctx, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal("Razlog odbijanja je obavezan.", result.Error);
    }

    [Fact]
    public void PickupDeadline_IsSetOnlyWhileApproved()
    {
        var approvedAt = new DateTime(2026, 6, 10, 8, 30, 0, DateTimeKind.Utc);
        var rental = new InstrumentRental(1, 1, 1, approvedAt.AddDays(-1), null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Approved, approvedAt, userId: 10, approvedAt: approvedAt);

        Assert.Equal(new DateTime(2026, 6, 12, 8, 30, 0, DateTimeKind.Utc), rental.PickupDeadline);

        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m };
        var result = rental.Transition(RentalTrigger.Pickup, ctx, approvedAt);

        Assert.True(result.IsSuccess);
        Assert.Null(rental.PickupDeadline);

        var pending = new InstrumentRental(1, 1, 1, approvedAt, null);
        Assert.Null(pending.PickupDeadline);
    }

    [Fact]
    public void Approve_CopiesTheOverduePenaltyMultiplier_AndExposesTheDailyPenaltyFee()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 60m, OverduePenaltyMultiplier = 0.75m };

        var result = rental.Transition(RentalTrigger.Approve, ctx, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.75m, rental.OverduePenaltyMultiplier);
        Assert.Equal(1.50m, rental.DailyPenaltyFee);
    }

    [Fact]
    public void Pickup_SetsDueAtAndRenewalOpensAt()
    {
        var pickedUpAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var rental = new InstrumentRental(1, 1, 1, pickedUpAt.AddDays(-1), null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Approved, pickedUpAt, approvedAt: pickedUpAt);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 60m, OverduePenaltyMultiplier = 0.5m };

        var result = rental.Transition(RentalTrigger.Pickup, ctx, pickedUpAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2026, 3, 31, 10, 0, 0, DateTimeKind.Utc), rental.DueAt);
        Assert.Equal(new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc), rental.RenewalOpensAt);
    }

    [Fact]
    public void RenewalOpensAt_IsOnlySetForActiveRentals()
    {
        var approved = BuildRentalIn(InstrumentRentalStatus.Approved);
        var completed = BuildRentalIn(InstrumentRentalStatus.Completed);

        Assert.Null(approved.RenewalOpensAt);
        Assert.Null(completed.RenewalOpensAt);
        Assert.NotNull(completed.DueAt);
    }

    [Fact]
    public void Renew_AtWindowOpen_Succeeds()
    {
        var rental = CreateActiveRental(new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc));
        var ctx = CreateStudentContext();

        var result = rental.Transition(RentalTrigger.Renew, ctx, new DateTime(2026, 3, 26, 10, 0, 0, DateTimeKind.Utc));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(new DateTime(2026, 4, 30, 10, 0, 0, DateTimeKind.Utc), rental.DueAt);
        Assert.Equal(InstrumentRentalStatus.Active, rental.RentalStatus);
    }

    [Fact]
    public void Renew_OneMinuteBeforeWindow_Fails()
    {
        var rental = CreateActiveRental(new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc));
        var ctx = CreateStudentContext();

        var result = rental.Transition(RentalTrigger.Renew, ctx, new DateTime(2026, 3, 26, 9, 59, 0, DateTimeKind.Utc));

        Assert.False(result.IsSuccess);
        Assert.Equal("Produženje je moguće samo u zadnjih 5 dana trenutnog roka.", result.Error);
        Assert.Equal(new DateTime(2026, 3, 31, 10, 0, 0, DateTimeKind.Utc), rental.DueAt);
    }

    [Fact]
    public void Renew_LastMinuteBeforeDue_Succeeds()
    {
        var rental = CreateActiveRental(new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc));
        var ctx = CreateStudentContext();

        var result = rental.Transition(RentalTrigger.Renew, ctx, new DateTime(2026, 3, 31, 9, 59, 0, DateTimeKind.Utc));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(new DateTime(2026, 4, 30, 10, 0, 0, DateTimeKind.Utc), rental.DueAt);
    }

    [Fact]
    public void Renew_AtDue_Fails()
    {
        var rental = CreateActiveRental(new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc));
        var ctx = CreateStudentContext();

        var result = rental.Transition(RentalTrigger.Renew, ctx, new DateTime(2026, 3, 31, 10, 0, 0, DateTimeKind.Utc));

        Assert.False(result.IsSuccess);
        Assert.Equal("Rok iznajmljivanja je istekao pa produženje više nije moguće.", result.Error);
        Assert.Equal(new DateTime(2026, 3, 31, 10, 0, 0, DateTimeKind.Utc), rental.DueAt);
    }

    [Fact]
    public void Renew_Twice_AddsTwoTerms()
    {
        var rental = CreateActiveRental(new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc));
        var ctx = CreateStudentContext();

        Assert.True(rental.Transition(RentalTrigger.Renew, ctx, new DateTime(2026, 3, 27, 10, 0, 0, DateTimeKind.Utc)).IsSuccess);
        Assert.True(rental.Transition(RentalTrigger.Renew, ctx, new DateTime(2026, 4, 26, 10, 0, 0, DateTimeKind.Utc)).IsSuccess);

        Assert.Equal(new DateTime(2026, 5, 30, 10, 0, 0, DateTimeKind.Utc), rental.DueAt);
    }

    [Fact]
    public void Renew_AsStoreEmployee_IsDenied()
    {
        var rental = CreateActiveRental(new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc));
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 60m, OverduePenaltyMultiplier = 0.5m };

        var result = rental.Transition(RentalTrigger.Renew, ctx, new DateTime(2026, 3, 27, 10, 0, 0, DateTimeKind.Utc));

        Assert.False(result.IsSuccess);
        Assert.Equal("Nemate pravo nad ovim zahtjevom.", result.Error);
    }

    [Fact]
    public void Renew_ApprovedRental_FailsWithWrongStateMessage()
    {
        var rental = BuildRentalIn(InstrumentRentalStatus.Approved);
        var ctx = CreateStudentContext();

        var result = rental.Transition(RentalTrigger.Renew, ctx, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal("Samo aktivno iznajmljivanje se može produžiti.", result.Error);
    }

    [Fact]
    public void Complete_BeforeDue_Fails()
    {
        var rental = CreateActiveRental(new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc));
        var ctx = CreateStoreContext();

        var result = rental.Transition(RentalTrigger.Complete, ctx, new DateTime(2026, 3, 31, 9, 59, 0, DateTimeKind.Utc));

        Assert.False(result.IsSuccess);
        Assert.Equal("Rok iznajmljivanja još nije istekao. Raniji povrat evidentirajte kao prijevremeni povrat.", result.Error);
    }

    [Fact]
    public void Complete_AtDue_Succeeds()
    {
        var rental = CreateActiveRental(new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc));
        var ctx = CreateStoreContext();

        var result = rental.Transition(RentalTrigger.Complete, ctx, new DateTime(2026, 3, 31, 10, 0, 0, DateTimeKind.Utc));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(InstrumentRentalStatus.Completed, rental.RentalStatus);
    }

    [Fact]
    public void ReturnEarly_BeforeDue_Succeeds()
    {
        var rental = CreateActiveRental(new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc));
        var ctx = CreateStoreContext();

        var result = rental.Transition(RentalTrigger.ReturnEarly, ctx, new DateTime(2026, 3, 31, 9, 59, 0, DateTimeKind.Utc));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(InstrumentRentalStatus.ReturnedEarly, rental.RentalStatus);
    }

    [Fact]
    public void ReturnEarly_AtDue_Fails()
    {
        var rental = CreateActiveRental(new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc));
        var ctx = CreateStoreContext();

        var result = rental.Transition(RentalTrigger.ReturnEarly, ctx, new DateTime(2026, 3, 31, 10, 0, 0, DateTimeKind.Utc));

        Assert.False(result.IsSuccess);
        Assert.Equal("Rok iznajmljivanja je istekao. Povrat evidentirajte kao završetak iznajmljivanja.", result.Error);
    }

    // The only allowed moves, written from the rental rules: (from, trigger, actor) -> new status.
    private static readonly (InstrumentRentalStatus From, RentalTrigger Trigger, RentalActor Actor, InstrumentRentalStatus To)[] AllowedMoves =
    [
        (InstrumentRentalStatus.Pending, RentalTrigger.Approve, RentalActor.StoreEmployee, InstrumentRentalStatus.Approved),
        (InstrumentRentalStatus.Pending, RentalTrigger.Reject, RentalActor.StoreEmployee, InstrumentRentalStatus.Rejected),
        (InstrumentRentalStatus.Pending, RentalTrigger.Cancel, RentalActor.Student, InstrumentRentalStatus.Canceled),
        (InstrumentRentalStatus.Approved, RentalTrigger.Pickup, RentalActor.StoreEmployee, InstrumentRentalStatus.Active),
        (InstrumentRentalStatus.Approved, RentalTrigger.Cancel, RentalActor.Student, InstrumentRentalStatus.Canceled),
        (InstrumentRentalStatus.Approved, RentalTrigger.Cancel, RentalActor.StoreEmployee, InstrumentRentalStatus.Canceled),
        (InstrumentRentalStatus.Approved, RentalTrigger.ExpirePickup, RentalActor.System, InstrumentRentalStatus.Rejected),
        (InstrumentRentalStatus.Active, RentalTrigger.Complete, RentalActor.StoreEmployee, InstrumentRentalStatus.Completed),
        (InstrumentRentalStatus.Active, RentalTrigger.ReturnEarly, RentalActor.StoreEmployee, InstrumentRentalStatus.ReturnedEarly),
        (InstrumentRentalStatus.Active, RentalTrigger.Renew, RentalActor.Student, InstrumentRentalStatus.Active),
    ];

    public static TheoryData<InstrumentRentalStatus, RentalTrigger, RentalActor> EveryCombination()
    {
        var data = new TheoryData<InstrumentRentalStatus, RentalTrigger, RentalActor>();

        foreach (var status in Enum.GetValues<InstrumentRentalStatus>())
        foreach (var trigger in Enum.GetValues<RentalTrigger>())
        foreach (var actor in Enum.GetValues<RentalActor>())
        {
            data.Add(status, trigger, actor);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(EveryCombination))]
    public void Transition_IsAllowedOnlyForTheRentalRules(InstrumentRentalStatus from, RentalTrigger trigger, RentalActor actor)
    {
        var rental = BuildRentalIn(from);
        var context = new RentalTransitionContext { UserId = 10, Actor = actor, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m, ResponseNote = "Razlog." };
        var expected = AllowedMoves.Where(m => m.From == from && m.Trigger == trigger && m.Actor == actor).ToList();
        var at = from == InstrumentRentalStatus.Active && trigger == RentalTrigger.Complete ? Now.AddDays(3) : Now;

        var result = rental.Transition(trigger, context, at);

        if (expected.Count == 0)
        {
            Assert.False(result.IsSuccess);
            Assert.Equal(from, rental.RentalStatus);
        }
        else
        {
            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(expected[0].To, rental.RentalStatus);
        }
    }

    [Fact]
    public void Complete_WithoutNote_KeepsTheNoteFromApproval()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Active, Now, note: "Dolazi u petak.", userId: 10);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m, OverduePenaltyMultiplier = 0.5m };

        rental.Transition(RentalTrigger.Complete, ctx, Now.AddDays(30));

        Assert.Equal("Dolazi u petak.", rental.Note);
    }

    private static InstrumentRental BuildRentalIn(InstrumentRentalStatus status)
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);

        switch (status)
        {
            case InstrumentRentalStatus.Completed:
                RentalTestData.AdvanceTo(rental, status, Now, userId: 10, approvedAt: Now.AddDays(-35), pickedUpAt: Now.AddDays(-34), finishedAt: Now.AddDays(-1));
                break;
            case InstrumentRentalStatus.ReturnedEarly:
                RentalTestData.AdvanceTo(rental, status, Now, userId: 10, approvedAt: Now.AddDays(-10), pickedUpAt: Now.AddDays(-9), finishedAt: Now.AddDays(-5));
                break;
            case InstrumentRentalStatus.Active:
                RentalTestData.AdvanceTo(rental, status, Now, userId: 10, approvedAt: Now.AddDays(-27), pickedUpAt: Now.AddDays(-27));
                break;
            default:
                RentalTestData.AdvanceTo(rental, status, Now, userId: 10, approvedAt: Now.AddDays(-3));
                break;
        }

        return rental;
    }

    private static InstrumentRental CreateActiveRental(DateTime pickedUpAt)
    {
        var rental = new InstrumentRental(1, 1, 1, pickedUpAt.AddDays(-1), null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Active, pickedUpAt, fee: 60m, approvedAt: pickedUpAt, pickedUpAt: pickedUpAt);
        return rental;
    }

    private static RentalTransitionContext CreateStudentContext() =>
        new() { UserId = 10, Actor = RentalActor.Student, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 60m, OverduePenaltyMultiplier = 0.5m };

    private static RentalTransitionContext CreateStoreContext() =>
        new() { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 60m, OverduePenaltyMultiplier = 0.5m };
}
