using eNote.Domain.Entities;

namespace eNote.Application.Features.InstrumentRentals.StateMachine;

public interface IRentalStateMachine
{
    Task<RentalTransitionResult> FireAsync(InstrumentRental rental, RentalTrigger trigger, RentalTransitionContext context, CancellationToken cancellationToken = default);
}
