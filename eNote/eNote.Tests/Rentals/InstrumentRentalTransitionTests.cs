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
            MonthlyFee = 50m,
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
            MonthlyFee = 50m,
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
            MonthlyFee = 50m,
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
            MonthlyFee = 50m,
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
            MonthlyFee = 50m
        };

        var result = rental.Transition(RentalTrigger.Pickup, context, Now);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Complete_FromPending_ReturnsInvalidTransition()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m };

        var result = rental.Transition(RentalTrigger.Complete, ctx, Now);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Approve_Fails_WhenLockConflict()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = true, IsInstrumentInactive = false, MonthlyFee = 50m };

        var result = rental.Transition(RentalTrigger.Approve, ctx, Now);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Approve_Succeeds_SetsApprovedStatus()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m };

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
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m };

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
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m };

        var result = rental.Transition(RentalTrigger.Complete, ctx, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.Completed, rental.RentalStatus);
        Assert.NotNull(rental.ReturnedAt);
    }

    [Fact]
    public void ReturnEarly_Succeeds_SetsReturnedEarlyStatus()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Active, Now, userId: 10);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m };

        var result = rental.Transition(RentalTrigger.ReturnEarly, ctx, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.ReturnedEarly, rental.RentalStatus);
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
        (InstrumentRentalStatus.Active, RentalTrigger.Complete, RentalActor.StoreEmployee, InstrumentRentalStatus.Completed),
        (InstrumentRentalStatus.Active, RentalTrigger.ReturnEarly, RentalActor.StoreEmployee, InstrumentRentalStatus.ReturnedEarly),
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
        var context = new RentalTransitionContext { UserId = 10, Actor = actor, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m, ResponseNote = "Razlog." };
        var expected = AllowedMoves.Where(m => m.From == from && m.Trigger == trigger && m.Actor == actor).ToList();

        var result = rental.Transition(trigger, context, Now);

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
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, IsInstrumentInactive = false, MonthlyFee = 50m };

        rental.Transition(RentalTrigger.Complete, ctx, Now);

        Assert.Equal("Dolazi u petak.", rental.Note);
    }

    private static InstrumentRental BuildRentalIn(InstrumentRentalStatus status)
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);

        RentalTestData.AdvanceTo(rental, status, Now, userId: 10);

        return rental;
    }
}
