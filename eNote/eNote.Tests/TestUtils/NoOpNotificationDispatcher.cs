using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;

namespace eNote.Tests.TestUtils;

public sealed class NoOpNotificationDispatcher : IRentalNotificationDispatcher
{
    public Task DispatchCreatedAsync(InstrumentRentalDto rental, int studentUserId) => Task.CompletedTask;

    public Task DispatchTransitionAsync(InstrumentRentalDto rental, RentalTrigger trigger, int actorUserId) => Task.CompletedTask;

    public Task DispatchPaymentRefundedAsync(InstrumentRentalDto rental, long refundedCents, int actorUserId) => Task.CompletedTask;
}
