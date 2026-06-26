using eNote.Domain.Entities.Rentals;

namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public interface IRentalStateMachine
{
    Task<RentalTransitionResult> FireAsync(InstrumentRental rental, RentalTrigger trigger, RentalTransitionContext context, CancellationToken cancellationToken = default);
}
