using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Rentals;

public sealed class RentalStateMachineTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
    private readonly RentalStateMachine _stateMachine = new(new FixedClock(Now));

    [Fact]
    public void Reject_FromPending_SetsRejectedStatus()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, "note");
        var context = new RentalTransitionContext
        {
            UserId = 10,
            Actor = RentalActor.StoreEmployee,
            HasInstrumentLockConflict = false,
            MonthlyFee = 50m,
            IsInstrumentActive = true
        };

        var result = _stateMachine.Fire(rental, RentalTrigger.Reject, context);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.Rejected, rental.RentalStatus);
        Assert.NotNull(rental.RejectedAt);
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
            MonthlyFee = 50m,
            IsInstrumentActive = true
        };

        var result = _stateMachine.Fire(rental, RentalTrigger.Cancel, context);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.Canceled, rental.RentalStatus);
        Assert.NotNull(rental.ReturnedAt);
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
            MonthlyFee = 50m,
            IsInstrumentActive = true
        };

        var result = _stateMachine.Fire(rental, RentalTrigger.Pickup, context);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Complete_FromPending_ReturnsInvalidTransition()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, MonthlyFee = 50m, IsInstrumentActive = true };

        var result = _stateMachine.Fire(rental, RentalTrigger.Complete, ctx);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Approve_Fails_WhenInstrumentInactive()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, MonthlyFee = 50m, IsInstrumentActive = false };

        var result = _stateMachine.Fire(rental, RentalTrigger.Approve, ctx);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Approve_Fails_WhenLockConflict()
    {
        var instrument = new Instrument("M", "MFR", null, null, 1, 1);
        var rental = InstrumentRental.CreateWithInstrument(1, 1, 1, Now, null, instrument);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = true, MonthlyFee = 50m, IsInstrumentActive = true };

        var result = _stateMachine.Fire(rental, RentalTrigger.Approve, ctx);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Approve_Succeeds_SetsApprovedStatus()
    {
        var instrument = new Instrument("M", "MFR", null, null, 1, 1);
        var rental = InstrumentRental.CreateWithInstrument(1, 1, 1, Now, null, instrument);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, MonthlyFee = 50m, IsInstrumentActive = true };

        var result = _stateMachine.Fire(rental, RentalTrigger.Approve, ctx);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.Approved, rental.RentalStatus);
        Assert.Equal(50m, rental.Fee);
    }

    [Fact]
    public void Pickup_Succeeds_SetsActiveStatus()
    {
        var instrument = new Instrument("M", "MFR", null, null, 1, 1);
        var rental = InstrumentRental.CreateWithInstrument(1, 1, 1, Now, null, instrument);
        rental.Approve(50m, null, Now, 10);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, MonthlyFee = 50m, IsInstrumentActive = true };

        var result = _stateMachine.Fire(rental, RentalTrigger.Pickup, ctx);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.Active, rental.RentalStatus);
        Assert.NotNull(rental.PickedUpAt);
    }

    [Fact]
    public void Complete_Succeeds_SetsCompletedStatus()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        rental.Approve(50m, null, Now, 10);
        rental.Pickup(Now);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, MonthlyFee = 50m, IsInstrumentActive = true };

        var result = _stateMachine.Fire(rental, RentalTrigger.Complete, ctx);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.Completed, rental.RentalStatus);
        Assert.NotNull(rental.ReturnedAt);
    }

    [Fact]
    public void ReturnEarly_Succeeds_SetsReturnedEarlyStatus()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        rental.Approve(50m, null, Now, 10);
        rental.Pickup(Now);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, MonthlyFee = 50m, IsInstrumentActive = true };

        var result = _stateMachine.Fire(rental, RentalTrigger.ReturnEarly, ctx);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.ReturnedEarly, rental.RentalStatus);
    }

}
