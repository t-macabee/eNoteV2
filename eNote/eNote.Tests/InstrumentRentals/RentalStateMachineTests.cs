using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Time;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;
using eNote.Domain.Enums;
using eNote.Domain.Shared;
using eNote.Tests.TestUtils;
using Xunit;
using eNote.Domain.Entities.Rentals;

namespace eNote.Tests.InstrumentRentals;

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
            HasInstrumentLockConflict = false
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
            HasInstrumentLockConflict = false
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
            HasInstrumentLockConflict = false
        };

        var result = _stateMachine.Fire(rental, RentalTrigger.Pickup, context);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }
}
