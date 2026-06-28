using eNote.Domain.Entities;

namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public interface IRentalStateMachine
{
    RentalTransitionResult Fire(InstrumentRental rental, RentalTrigger trigger, RentalTransitionContext context);
}
