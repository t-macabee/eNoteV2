using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

namespace eNote.Application.Common.Interfaces;

public interface IRentalNotificationDispatcher
{
    Task DispatchCreatedAsync(InstrumentRentalDto rental, int studentUserId, CancellationToken cancellationToken = default);

    Task DispatchTransitionAsync(
        InstrumentRentalDto rental,
        RentalTrigger trigger,
        int actorUserId,
        CancellationToken cancellationToken = default);
}
