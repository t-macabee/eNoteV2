using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Time;
using eNote.Application.Features.InstrumentRentals.StateMachine;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Xunit;

namespace eNote.Tests.InstrumentRentals;

public sealed class RentalStateMachineTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
    private readonly RentalStateMachine _stateMachine = new(new SystemClock());

    [Fact]
    public async Task Reject_FromPending_SetsRejectedStatus()
    {
        var rental = new InstrumentRental(1, 1, Now, "note");
        var context = new RentalTransitionContext
        {
            UserId = 10,
            Actor = RentalActor.StoreEmployee,
            Db = new NoConflictDbContext()
        };

        await _stateMachine.FireAsync(rental, RentalTrigger.Reject, context);

        Assert.Equal(InstrumentRentalStatus.Rejected, rental.RentalStatus);
        Assert.NotNull(rental.RejectedAt);
    }

    [Fact]
    public async Task Cancel_FromPending_WithoutPickup_SetsCanceledStatus()
    {
        var rental = new InstrumentRental(1, 1, Now, null);
        var context = new RentalTransitionContext
        {
            UserId = 5,
            Actor = RentalActor.Student,
            Db = new NoConflictDbContext()
        };

        await _stateMachine.FireAsync(rental, RentalTrigger.Cancel, context);

        Assert.Equal(InstrumentRentalStatus.Canceled, rental.RentalStatus);
        Assert.NotNull(rental.ReturnedAt);
    }

    [Fact]
    public async Task FireAsync_WithInvalidTransition_ThrowsBusinessException()
    {
        var rental = new InstrumentRental(1, 1, Now, null);
        var context = new RentalTransitionContext
        {
            UserId = 10,
            Actor = RentalActor.StoreEmployee,
            Db = new NoConflictDbContext()
        };

        await Assert.ThrowsAsync<BusinessException>(() => _stateMachine.FireAsync(rental, RentalTrigger.Pickup, context));
    }
}