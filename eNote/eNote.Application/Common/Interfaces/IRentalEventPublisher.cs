using eNote.Application.Features.InstrumentRentals;
using eNote.Application.Features.InstrumentRentals.StateMachine;

namespace eNote.Application.Common.Interfaces;

public interface IRentalEventPublisher
{
    Task PublishCreatedAsync(InstrumentRentalDto rental, int studentUserId, CancellationToken cancellationToken = default);
    Task PublishTransitionAsync(InstrumentRentalDto rental, RentalTrigger trigger, int actorUserId, CancellationToken cancellationToken = default);
}
