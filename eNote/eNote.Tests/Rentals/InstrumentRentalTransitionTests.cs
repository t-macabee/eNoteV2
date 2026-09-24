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
            MonthlyFee = 50m,
            ResponseNote = "Instrument je na servisu."
        };

        var result = rental.Transition(RentalTrigger.Reject, context, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.Rejected, rental.RentalStatus);
        Assert.NotNull(rental.RejectedAt);
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
            MonthlyFee = 50m
        };

        var result = rental.Transition(RentalTrigger.Cancel, context, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.Canceled, rental.RentalStatus);
        Assert.Null(rental.ReturnedAt);
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
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, MonthlyFee = 50m };

        var result = rental.Transition(RentalTrigger.Complete, ctx, Now);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Approve_Fails_WhenLockConflict()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = true, MonthlyFee = 50m };

        var result = rental.Transition(RentalTrigger.Approve, ctx, Now);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Approve_Succeeds_SetsApprovedStatus()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, MonthlyFee = 50m };

        var result = rental.Transition(RentalTrigger.Approve, ctx, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.Approved, rental.RentalStatus);
        Assert.Equal(50m, rental.Fee);
    }

    [Fact]
    public void Pickup_Succeeds_SetsActiveStatus()
    {
        var rental = new InstrumentRental(1, 1, 1, Now, null);
        rental.Approve(50m, null, Now, 10);
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, MonthlyFee = 50m };

        var result = rental.Transition(RentalTrigger.Pickup, ctx, Now);

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
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, MonthlyFee = 50m };

        var result = rental.Transition(RentalTrigger.Complete, ctx, Now);

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
        var ctx = new RentalTransitionContext { UserId = 10, Actor = RentalActor.StoreEmployee, HasInstrumentLockConflict = false, MonthlyFee = 50m };

        var result = rental.Transition(RentalTrigger.ReturnEarly, ctx, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(InstrumentRentalStatus.ReturnedEarly, rental.RentalStatus);
    }
}
