using eNote.Domain.Entities.Rentals;
using eNote.Domain.Shared;

namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public interface IRentalStateMachine
{
    Result<RentalTransitionResult> Fire(InstrumentRental rental, RentalTrigger trigger, RentalTransitionContext context);
}
