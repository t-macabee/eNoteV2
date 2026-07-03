using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

namespace eNote.Tests.TestUtils;

public sealed class RecordingNotificationDispatcher : IRentalNotificationDispatcher
{
    public List<(InstrumentRentalDto Rental, int StudentUserId)> CreatedCalls { get; } = [];
    public List<(InstrumentRentalDto Rental, RentalTrigger Trigger, int ActorUserId)> TransitionCalls { get; } = [];

    public Task DispatchCreatedAsync(InstrumentRentalDto rental, int studentUserId)
    {
        CreatedCalls.Add((rental, studentUserId));
        return Task.CompletedTask;
    }

    public Task DispatchTransitionAsync(InstrumentRentalDto rental, RentalTrigger trigger, int actorUserId)
    {
        TransitionCalls.Add((rental, trigger, actorUserId));
        return Task.CompletedTask;
    }
}
