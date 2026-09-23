using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;

namespace eNote.Tests.TestUtils;

public sealed class NoOpNotificationDispatcher : IRentalNotificationDispatcher
{
    public Task DispatchCreatedAsync(InstrumentRentalDto rental, int studentUserId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DispatchTransitionAsync(InstrumentRentalDto rental, RentalTrigger trigger, int actorUserId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DispatchPaymentRefundedAsync(InstrumentRentalDto rental, long refundedCents, string currency, int actorUserId) => Task.CompletedTask;
}
