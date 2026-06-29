using eNote.Application.Common.Interfaces;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

namespace eNote.Tests.TestUtils;

public sealed class NoOpNotificationDispatcher : IRentalNotificationDispatcher
{
    public Task DispatchCreatedAsync(InstrumentRentalDto rental, int studentUserId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DispatchTransitionAsync(InstrumentRentalDto rental, RentalTrigger trigger, int actorUserId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
